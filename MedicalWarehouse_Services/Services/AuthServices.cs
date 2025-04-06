using AutoMapper;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedicalWarehouse_Services.Services
{
    public class AuthServices : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _jwtSettings;
        private readonly ILogger<AuthServices> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly TokenProvider _tokenProvider;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly IAuthRepository _authRepository;

        public AuthServices
            (
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AuthServices> logger,
            IConfiguration configuration,
            TokenProvider tokenProvider,
            IHttpContextAccessor contextAccessor,
            IMapper mapper,
            IAuthRepository authRepository
            )
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _tokenProvider = tokenProvider;
            _jwtSettings = _configuration.GetSection("JWT");
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _authRepository = authRepository;
        }

        public async Task DeleteUserProfile(string userId)
        {
            await _authRepository.Delete(userId);
        }

        public async Task<List<UserResponseModel>> GetAllUsers()
        {
            var listUser = await _authRepository.GetAll();
            return listUser.Where(user => user.Role != UserRoles.ADMIN).ToList();
        }

        public async Task<UserResponseModel> GetUserById(string id)
        {
            var user = await _authRepository.GetById(id);
            if (user == null)
            {
                throw new Exception($"Không có người dùng thích hợp với Id: {id} ");
            }
            return _mapper.Map<UserResponseModel>(user);
        }

        public async Task<LoginResponse> Login(LoginModel login)
        {
            var isExisted = await _userManager.FindByEmailAsync(login.Email);
            if (isExisted == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Email chưa được đăng ký!"
                };
            }
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(isExisted);
            if (!isEmailConfirmed)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Tài khoản chưa được xác thực!"
                };
            }
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(isExisted, login.Password);
            if (!isPasswordCorrect)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Sai mật khẩu !"
                };
            }

            if (isExisted.VerifyTokenExpired < DateTime.Now.ToUniversalTime())
            {
                isExisted.VerifyToken = _tokenProvider.CreateRandomToken();

                var updateUserResult = await _userManager.UpdateAsync(isExisted);
                if (!updateUserResult.Succeeded)
                {
                    var errorString = "Cập nhật người dùng thất bại vì: " +
                                      string.Join(" # ", updateUserResult.Errors.Select(e => e.Description));
                    return new LoginResponse { Success = false, RefreshToken = errorString };
                }
            }
            return new LoginResponse
            {
                Success = true,
                RefreshToken = isExisted.VerifyToken,
                AccessToken = await _tokenProvider.Create(isExisted),
                ExpiredTime = DateTime.Now.AddMinutes(60).ToUniversalTime()
            };
        }

        public async Task<LoginResponse> MakeStaff(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Không tìm thấy người dùng"
                };
            }

            var userRole = await _userManager.GetRolesAsync(user);
            if (userRole.Contains("STAFF"))
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Người dùng đã là STAFF"
                };
            }

            foreach (var role in userRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }

            await _userManager.AddToRoleAsync(user, UserRoles.STAFF);
            user.Role = UserRoles.STAFF;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errorString = "Cập nhật người dùng thất bại vì: " +
                                  string.Join(" # ", updateResult.Errors.Select(e => e.Description));
                return new LoginResponse { Success = false, RefreshToken = errorString };
            }

            return new LoginResponse
            {
                Success = true,
                RefreshToken = "Tài khoản đã được nâng cấp thành Staff"
            };
        }

        public async Task<UserResponseModel> Profile()
        {
            var userId = _contextAccessor.HttpContext?.User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            // Tìm user từ database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            return _mapper.Map<UserResponseModel>(user);
        }

        public async Task<LoginResponse> Refresh(string refreshToken)
        {
            var currUid = _contextAccessor.HttpContext.User.FindFirst("uid")?.Value;
            var user = await _userManager.FindByIdAsync(currUid);
            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Invalid token",
                };
            }

            if (user.VerifyTokenExpired < DateTime.Now.ToUniversalTime())
            {
                user.VerifyToken = await _tokenProvider.Create(user);

                var updateUserResult = await _userManager.UpdateAsync(user);
                if (!updateUserResult.Succeeded)
                {
                    var errorString = "Cập nhật người dùng thất bại vì: " +
                                      string.Join(" # ", updateUserResult.Errors.Select(e => e.Description));
                    return new LoginResponse { Success = false, RefreshToken = errorString };
                }
            }

            return new LoginResponse
            {
                Success = true,
                RefreshToken = user.VerifyToken,
                AccessToken = await _tokenProvider.Create(user),
                ExpiredTime = DateTime.Now.AddMinutes(30).ToUniversalTime()
            };
        }

        public async Task<LoginResponse> Register(LoginModel register, string userName)
        {
            var isExisted = await _userManager.FindByEmailAsync(register.Email);

            if (isExisted != null)
            {
                return new LoginResponse
                {
                    Success = false,
                    RefreshToken = "Địa chỉ Email đã tồn tại ",
                };
            }

            User newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = register.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                VerifyTokenExpired = DateTime.Now.AddHours(24).ToUniversalTime(),
                CreatedDate = DateTime.Now.ToUniversalTime(),
                Role = UserRoles.PHARMACY,
            };

            var createUser = await _userManager.CreateAsync(newUser, register.Password);
            if (!createUser.Succeeded)
            {
                var errorString = "Tạo mới người dùng không thành công bởi vì: " +
                                  string.Join(" # ", createUser.Errors.Select(e => e.Description));
                return new LoginResponse { Success = false, RefreshToken = errorString };
            }

            await _userManager.AddToRoleAsync(newUser, UserRoles.PHARMACY);

            var verifyToken = _tokenProvider.CreateRandomToken();
            newUser.VerifyToken = verifyToken;

            var updateUserResult = await _userManager.UpdateAsync(newUser);
            if (!updateUserResult.Succeeded)
            {
                var errorString = "Cập nhật người dùng thất bại vì: " +
                                  string.Join(" # ", updateUserResult.Errors.Select(e => e.Description));
                return new LoginResponse { Success = false, RefreshToken = errorString };
            }

            return new LoginResponse
            {
                Success = true,
                RefreshToken = "Tạo mới tài khoản thành công ! "
            };
        }

        public async Task<LoginResponse> SeedRoles()
        {
            var isAdminRoleExists = await _roleManager.RoleExistsAsync(UserRoles.ADMIN);
            var isStaffRoleExists = await _roleManager.RoleExistsAsync(UserRoles.STAFF);
            var isPharmacyRoleExists = await _roleManager.RoleExistsAsync(UserRoles.PHARMACY);

            if (isAdminRoleExists && isStaffRoleExists && isPharmacyRoleExists)
            {
                return new LoginResponse
                {
                    Success = true,
                    AccessToken = "Seed roles already",
                    RefreshToken = null
                };
            }

            await _roleManager.CreateAsync(new IdentityRole(UserRoles.ADMIN));
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.STAFF));
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.PHARMACY));

            return new LoginResponse
            {
                Success = true,
                AccessToken = "Seed roles successfully",
                RefreshToken = null
            };
        }

        public async Task<bool> VerifyEmail(string email)
        {
            var userExist = await _userManager.FindByEmailAsync(email);
            if (userExist == null)
            {
                return false; // Trả về false nếu không tìm thấy user
            }

            bool isConfirmed = await _userManager.IsEmailConfirmedAsync(userExist);
            if (isConfirmed)
            {
                throw new InvalidOperationException("Email đã được xác nhận .");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(userExist);

            // Thực hiện xác nhận email
            var result = await _userManager.ConfirmEmailAsync(userExist, token);
            return result.Succeeded;
        }

    }
}
