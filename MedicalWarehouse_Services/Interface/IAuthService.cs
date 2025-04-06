using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
namespace MedicalWarehouse_Services.Interface;

public interface IAuthService
{
    Task<LoginResponse> SeedRoles();
    Task<LoginResponse> Login(LoginModel login);
    Task<LoginResponse> Register(LoginModel register, string userName);
    Task<LoginResponse> Refresh(string refreshToken);
    Task<UserResponseModel> Profile();
    Task<LoginResponse> MakeStaff(string userId);
    Task<bool> VerifyEmail(string email);
    Task<List<UserResponseModel>> GetAllUsers();
    Task<UserResponseModel> GetUserById(string id);
    Task DeleteUserProfile(string userId);
}

