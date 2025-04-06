using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MedicalWareHouse_Client.Pages
{
    public class UserDetailsModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;

        public UserDetailsModel(HttpClient httpClient, IOptions<DomainSetting> domainSetting, IHttpContextAccessor httpContext)
        {
            _httpClient = httpClient;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
        }

        [BindProperty]
        public UserResponseModel? UserInfo { get; set; }

        public bool IsAdminOrStaff { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login/LoginPage");
            }

            IsAdminOrStaff = GetUserRoles(token).Any(r => r == UserRoles.ADMIN || r == UserRoles.STAFF);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{GetCurrentDomain()}auth/profile");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<BaseResponse<UserResponseModel>>(content);

                    if (responseObject != null && responseObject.Success && responseObject.Result != null)
                    {
                        UserInfo = responseObject.Result;
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                return StatusCode(500);
            }

            return Page();
        }

        private string GetCurrentDomain() => _domainSetting.Local;

        private List<string> GetUserRoles(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            return roleClaims.Select(c => c.Value).ToList();
        }
    }
}
