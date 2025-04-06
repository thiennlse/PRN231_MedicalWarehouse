using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Enums;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Headers;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.OrderPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class DetailsModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseurl = "http://localhost:5273";

        public DetailsModel(HttpClient httpClient, IHttpContextAccessor httpContext)
        {
            _httpClient = httpClient;
            _httpContext = httpContext;
        }

        public OrderResponseModel Order { get; set; } = default!;
        public Dictionary<Guid, string> MedicalNames { get; set; } = new Dictionary<Guid, string>();
        public bool IsAdminOrStaff { get; private set; }
        public bool IsPharmacy { get; private set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            try
            {
                // Check user roles
                var userRoles = GetUserRoles();
                IsAdminOrStaff = userRoles.Any(r => r == UserRoles.ADMIN || r == UserRoles.STAFF);
                IsPharmacy = userRoles.Any(r => r == UserRoles.PHARMACY);
                
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{_baseurl}/order/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<BaseResponse<OrderResponseModel>>(content);
                    if (responseObject != null && responseObject.Success && responseObject.Result != null)
                    {
                        Order = responseObject.Result;
                        
                        // Load medical names for all medicinal products in the order
                        if (Order.OrderDetails != null && Order.OrderDetails.Any())
                        {
                            await LoadMedicalNames();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể tải thông tin đơn hàng.");
                    }
                }
                return Page();

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Lỗi yêu cầu HTTP: {e.Message}");
                ModelState.AddModelError(string.Empty, $"Lỗi kết nối đến API: {e.Message}");
                return Page(); 
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Console.WriteLine($"Lỗi phân tích JSON: {e.Message}");
                ModelState.AddModelError(string.Empty, $"Lỗi dữ liệu: {e.Message}");
                return Page(); 
            }
        }

        private List<string> GetUserRoles()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return new List<string>();
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            return roleClaims.Select(c => c.Value).ToList();
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task LoadMedicalNames()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseurl}/medical");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);
                    if (responseObject != null && responseObject.Success && responseObject.Results != null)
                    {
                        foreach (var medical in responseObject.Results)
                        {
                            MedicalNames[medical.Id] = medical.Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading medical names: {ex.Message}");
            }
        }

        public string GetMedicalName(Guid medicalId)
        {
            if (MedicalNames.ContainsKey(medicalId))
            {
                return MedicalNames[medicalId];
            }
            return "Unknown Medical";
        }
    }
}