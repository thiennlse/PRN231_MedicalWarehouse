using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RegisterModel : PageModel
{
    private readonly HttpClient _httpClient;

    public RegisterModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var registrationRequest = new
        {
            email = Input.Email,
            password = Input.Password
        };

        var json = JsonSerializer.Serialize(registrationRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            // Gửi yêu cầu đăng ký
            var response = await _httpClient.PostAsync($"http://localhost:5273/auth/register?username={Input.Name}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response.IsSuccessStatusCode && apiResponse?.Success == true)
            {
               
                var sendEmailResponse = await _httpClient.PostAsync(
                    $"http://localhost:5273/auth/send_email?toEmail={Uri.EscapeDataString(Input.Email)}",
                    null 
                );

                if (sendEmailResponse.IsSuccessStatusCode)
                {
                    TempData["RegisterSuccessMessage"] = apiResponse.Message ?? "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.";
                }
                else
                {
                    TempData["RegisterSuccessMessage"] = apiResponse.Message ?? "Đăng ký thành công, nhưng gửi email xác nhận thất bại.";
                }

                return RedirectToPage("/Login");
            }
            else
            {
                TempData["RegisterErrorMessage"] = apiResponse?.Message ?? "Đăng ký thất bại!";
                return Page();
            }
        }
        catch (Exception ex)
        {
            TempData["RegisterErrorMessage"] = $"Có lỗi xảy ra khi đăng ký: {ex.Message}";
            return Page();
        }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
