using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.MedicalPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IndexModel> _logger;
        private readonly DomainSetting _domainSetting;

        public IndexModel(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<IndexModel> logger, IOptions<DomainSetting> domainSetting)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _domainSetting = domainSetting.Value;
        }

        public List<MedicalResponseModel> Medical { get; set; } = new List<MedicalResponseModel>();

        [BindProperty]
        public MedicalResponseModel medical { get; set; } = default!;

        public bool CanCreateMedical { get; private set; }
        public bool CanEditMedical { get; private set; }
        public bool CanDeleteMedical { get; private set; }
        public bool IsPharmacy { get; private set; }

        private List<string> GetUserRoles()
        {
            var token = _httpContextAccessor?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return new List<string>();
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            return roleClaims.Select(c => c.Value).ToList();
        }

        public async Task OnGetAsync()
        {
            var roles = GetUserRoles();
            CanCreateMedical = roles.Contains("ADMIN");
            CanEditMedical = roles.Contains("ADMIN");
            CanDeleteMedical = roles.Contains("ADMIN");
            IsPharmacy = roles.Contains("PHARMACY");

            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{GetCurrentDomain()}medical");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = JsonSerializer.Deserialize<BaseResponse<MedicalResponseModel>>(content, options);
                    Medical = result?.Results ?? new List<MedicalResponseModel>();
                }
                else
                {
                    _logger.LogError("Failed to fetch medical data. Status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "HTTP Request Error");
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "JSON Deserialization Error");
            }
        }


        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private string GetCurrentDomain() => _domainSetting.Local;
    }
}