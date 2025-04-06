using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MedicalWareHouse_Client.Pages
{
    public class LoginModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        public LoginModel(IHttpContextAccessor httpContext)
        {
            _httpClient = new HttpClient();
            _httpContext = httpContext;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var loginRequest = new
            {
                Email = Input.Email,
                Password = Input.Password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:5273/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    HttpContext.Session.SetString("AccessToken", loginResponse.AccessToken);
                    HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken);

                    // Lấy thông tin user
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);
                    var profileResponse = await _httpClient.GetAsync("http://localhost:5273/auth/profile");

                    if (profileResponse.IsSuccessStatusCode)
                    {
                        var profileContent = await profileResponse.Content.ReadAsStringAsync();
                        var profileData = JsonSerializer.Deserialize<UserProfileResponse>(profileContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (profileData != null && profileData.Success)
                        {
                            HttpContext.Session.SetString("UserRole", profileData.Result.Role);
                            HttpContext.Session.SetString("UserName", profileData.Result.Name);
                            HttpContext.Session.SetString("UserId", profileData.Result.Id);
                            
                            // Thiết lập cookie xác thực
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, profileData.Result.Id),
                                new Claim(ClaimTypes.Name, profileData.Result.Name),
                                new Claim(ClaimTypes.Email, profileData.Result.Email),
                                new Claim(ClaimTypes.Role, profileData.Result.Role)
                            };

                            var claimsIdentity = new ClaimsIdentity(
                                claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = Input.RememberMe,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                            };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);
                        }
                        
                        if (profileData.Result.Role.Equals("PHARMACY"))
                        {
                            TempData["LoginSuccessMessage"] = "Đăng nhập thành công!";
                            return RedirectToPage("/MedicalPage/Index");
                        }
                    }

                    TempData["LoginSuccessMessage"] = "Đăng nhập thành công!";
                    return RedirectToPage("/AreaPage/Index");
                }
                else
                {
                    TempData["LoginErrorMessage"] = "Sai email hoặc mật khẩu .";
                    return Page();
                }
            }
            catch
            {
                TempData["LoginErrorMessage"] = "Tài khoản chưa được xác thực, hãy kiểm tra trong email để kích hoạt tài khoản !";
                return Page();
            }
        }


        public class LoginInputModel
        {
            [Required(ErrorMessage = "Email không thể bỏ trống ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu không thể bỏ trống ")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public class LoginResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string ExpiredTime { get; set; }
        }
        public class UserProfileResponse
        {
            public bool Success { get; set; }
            public UserProfile Result { get; set; }
        }

        public class UserProfile
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
    }
}