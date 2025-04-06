using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers; // Add this for AuthenticationHeaderValue
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.MedicalPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class DetailsModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;

        public DetailsModel(HttpClient httpClient, IOptions<DomainSetting> domainSetting, IHttpContextAccessor httpContext)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _domainSetting = domainSetting?.Value ?? throw new ArgumentNullException(nameof(domainSetting));
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

        [BindProperty]
        public MedicalResponseModel Medical { get; set; } = default!;

        public List<ShipmentReponseModel> Shipments { get; set; } = new();

        public Dictionary<Guid, string> SupplierNames { get; set; } = new();

        public bool IsAdminOrStaff { get; private set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue || id == Guid.Empty)
            {
                return NotFound();
            }

            IsAdminOrStaff = GetUserRoles().Contains(UserRoles.ADMIN) || GetUserRoles().Contains(UserRoles.STAFF);

            try
            {
                await Task.WhenAll(LoadMedicalAsync(id.Value), LoadSuppliersAsync(), LoadShipmentsAsync(id.Value));

                if (Medical == null)
                {
                    return NotFound();
                }

                Medical.SupplierName = SupplierNames.GetValueOrDefault(Medical.SupplierId, "Không có thông tin");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HTTP Request Error: {e.Message}");
                return StatusCode(500);
            }

            return Page();
        }

        private async Task LoadMedicalAsync(Guid id)
        {
            SetAuthorizationHeader(); // Add authorization
            var response = await _httpClient.GetAsync($"{GetCurrentDomain()}medical/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);
                if (responseObject?.Success == true && responseObject.Result != null)
                {
                    Medical = responseObject.Result;
                }
            }
        }

        private async Task LoadSuppliersAsync()
        {
            SetAuthorizationHeader(); // Add authorization
            var response = await _httpClient.GetAsync($"{GetCurrentDomain()}supplier");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);
                SupplierNames = suppliers?.ToDictionary(s => s.Id, s => s.Name) ?? new();
            }
            else
            {
                Console.WriteLine($"Failed to load suppliers. Status Code: {response.StatusCode}");
            }
        }

        private async Task LoadShipmentsAsync(Guid medicalId)
        {
            SetAuthorizationHeader(); // Add authorization
            var response = await _httpClient.GetAsync($"{GetCurrentDomain()}medical/shipment/{medicalId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Shipment API Response: {content}");
                var responseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(content);
                if (responseObject?.Success == true)
                {
                    Shipments = responseObject.Results ?? new List<ShipmentReponseModel>();
                }
            }
        }

        private string GetCurrentDomain() => _domainSetting.Local;

        private List<string> GetUserRoles()
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return new List<string>();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Access token not found in session. Please log in again.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}