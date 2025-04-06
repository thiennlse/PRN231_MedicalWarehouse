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
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.OrderPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContext,
            IWebHostEnvironment env,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
            _env = env;
            _logger = logger;
        }

        public IList<Order> Order { get; set; } = default!;
        public List<string> Roles { get; set; } = new List<string>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10; // Số lượng đơn hàng trên mỗi trang

        // Search property
        [BindProperty(SupportsGet = true)]
        public string SearchByName { get; set; }

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

        private void SetAuthorizationHeader(HttpClient client)
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private string GetCurrentDomain()
        {
            return _env.IsDevelopment() ? _domainSetting.Local : _domainSetting.Server;
        }

        public async Task OnGetAsync(int pageNumber = 1, string searchByName = null)
        {
            SearchByName = searchByName;
            Roles = GetUserRoles();

            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Where(o => o.IsDeleted != true); // Handle nullable boolean

            if (!string.IsNullOrEmpty(SearchByName))
            {
                query = query.Where(o => o.Name != null && o.Name.ToLower().Contains(SearchByName.ToLower()));
            }

            // Áp dụng OrderByDescending sau khi đã lọc
            query = query.OrderByDescending(o => o.CreatedDate);

            var allOrders = await query.ToListAsync();
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling((double)allOrders.Count / PageSize); 
            Order = allOrders.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            _logger.LogInformation("Loaded {Count} orders for page {Page}", Order.Count, CurrentPage);
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    SetAuthorizationHeader(client);
                    string baseUrl = GetCurrentDomain();
                    var response = await client.DeleteAsync($"{baseUrl}order/{id}"); // Ensure correct URL

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<BaseResponse<Order>>(responseContent);

                        if (result?.Success == true)
                        {
                            TempData["SuccessMessage"] = result.Message ?? "Xóa đơn hàng thành công";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = result?.Message ?? "Lỗi khi xóa đơn hàng";
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode} - {errorContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            // Preserve pagination and search state
            return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchByName = SearchByName });
        }
    }
}