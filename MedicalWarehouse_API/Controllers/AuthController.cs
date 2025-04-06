using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MedicalWarehouse_API.Controllers;

[Controller]
[Route("auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly CloudinaryService _cloudinaryService;
    private readonly SendEmailService _sendEmailService;

    public AuthController(IAuthService authService, CloudinaryService cloudinaryService, SendEmailService sendEmailService)
    {
        _authService = authService;
        _cloudinaryService = cloudinaryService;
        _sendEmailService = sendEmailService;
    }

    [HttpPost]
    [Route("seed_roles")]
    public async Task<IActionResult> SeedRoles()
    {
        var seedRole = await _authService.SeedRoles();
        return Ok(seedRole);
    }

    [HttpPost]
    [Route("send_email")]
    public async Task<IActionResult> SendEmail(string toEmail)
    {
        await _sendEmailService.SendEmailAsync(toEmail);
        return Ok();
    }

    [HttpGet]
    [Route("verify_email")]
    public async Task<IActionResult> VerifyEmail(string email)
    {
        try
        {
            var isVerify = await _authService.VerifyEmail(email);
            if (isVerify)
            {
                return Redirect("https://localhost:7143/Login");
            }
            return Ok(new BaseResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
            {
                Success = false,
                Result = null,
                Message = ex.Message
            });
        }
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody][Required] LoginModel register, string username)
    {
        var registerResult = await _authService.Register(register, username);
        if (!registerResult.Success)
        {
            return Ok(new BaseResponse<User>
            {
                Success = false,
                Message = registerResult.RefreshToken
            });
        }
        return Ok(new BaseResponse<User>
        {
            Success = true,
            Message = "Register successfully, please check your email to confirm"
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("upload_image")]
    public async Task<IActionResult> Image(IFormFile file)
    {
        try
        {
            var result = await _cloudinaryService.SaveImage(file);
            if (result != null)
            {
                return Ok(new BaseResponse<object>
                {
                    Success = true,
                    Result = result,
                    Message = "Upload image successfully"
                });
            }
            return Ok(new BaseResponse<object>
            {
                Success = false,
                Message = "Upload image failed"
            });
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
            {
                Success = false,
                Result = null,
                Message = ex.Message
            });
        }
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _authService.Profile();
        if (user == null)
        {
            return Ok(new BaseResponse<UserResponseModel>
            {
                Success = false,
                Message = "User not found"
            });
        }
        return Ok(new BaseResponse<UserResponseModel>
        {
            Success = true,
            Result = user,
            Message = "Retrived data successfully"
        });
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody][Required] LoginModel login)
    {
        var loginResult = await _authService.Login(login);
        if (!loginResult.Success)
        {
            return Ok(new BaseResponse<User>
            {
                Success = false,
                Message = loginResult.RefreshToken
            });
        }

        return Ok(new LoginResponse
        {
            Success = true,
            RefreshToken = loginResult.RefreshToken,
            AccessToken = loginResult.AccessToken,
            ExpiredTime = loginResult.ExpiredTime,
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
    [Route("make_staff")]
    public async Task<IActionResult> MakeAdmin([Required] string userId)
    {
        try
        {
            var makeAdminResult = await _authService.MakeStaff(userId);
            if (!makeAdminResult.Success)
            {
                return Ok(new BaseResponse<User>
                {
                    Success = false,
                    Message = makeAdminResult.RefreshToken
                });
            }
            return Ok(new BaseResponse<UserResponseModel>
            {
                Success = true,
                Message = makeAdminResult.RefreshToken
            });
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
            {
                Success = false,
                Result = null,
                Message = ex.Message
            });
        }
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("refresh")]
    public async Task<IActionResult> Refresh([Required] string refeshToken)
    {
        var refreshTokenResult = await _authService.Refresh(refeshToken);
        if (!refreshTokenResult.Success)
        {
            return Ok(new BaseResponse<User>
            {
                Success = false,
                Message = refreshTokenResult.RefreshToken
            });
        }

        return Ok(new LoginResponse
        {
            Success = true,
            RefreshToken = refreshTokenResult.RefreshToken,
            AccessToken = refreshTokenResult.AccessToken,
            ExpiredTime = refreshTokenResult.ExpiredTime,
        });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
    [Route("user")]
    public async Task<IActionResult> GetAllProfile()
    {
        try
        {
            var listUser = await _authService.GetAllUsers();
            if (listUser == null)
            {
                return Ok(new BaseResponse<UserResponseModel>
                {
                    Success = false,
                    Message = "Không tìm thấy profile nào"
                });
            }
            return Ok(new BaseResponse<UserResponseModel>
            {
                Success = true,
                Results = listUser,
                Message = "Truy cập data thành công"
            });
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
            {
                Success = false,
                Result = null,
                Message = ex.Message
            });
        }
    }
    [HttpDelete]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
    [Route("user/{id}")]
    public async Task<IActionResult> RemoveUserProfile(string id)
    {
        try
        {
            await _authService.DeleteUserProfile(id);
            return Ok(new BaseResponse<UserResponseModel>
            {
                Success = true,
                Message = $"Đã xóa profile với Id:{id}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
            {
                Success = false,
                Result = null,
                Message = ex.Message
            });
        }
    }
};

