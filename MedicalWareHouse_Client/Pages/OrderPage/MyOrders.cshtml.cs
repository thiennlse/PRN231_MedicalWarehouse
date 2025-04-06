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
    public class MyOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IWebHostEnvironment _env;

        public MyOrdersModel(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContext,
            IWebHostEnvironment env)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
            _env = env;
        }

        public IList<Order> Orders { get; set; } = default!;
        public List<string> Roles { get; set; } = new List<string>();
        public bool isAuthen { get; set; } = false;

        private void SetAuthorizationHeader()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    foreach (var claim in jwtToken.Claims)
                    {
                        Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                    }

                    var roles = jwtToken.Claims
                        .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                        .Select(c => c.Value)
                        .ToList();

                    if (roles.Any())
                    {
                        isAuthen = true;
                        Roles = roles;
                    }
                    else
                    {
                        throw new InvalidOperationException("Không tìm thấy thông tin vai trò trong token");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Lỗi khi đọc token: {ex.Message}");
                }
            }
            else
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
        }

        private string GetUserId()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "uid")?.Value ??
                            jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value ??
                            jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Không tìm thấy ID người dùng trong token");
                }

                return userId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy ID người dùng: {ex.Message}");
            }
        }

        public string GetCurrentDomain()
        {
            return _env.IsDevelopment() ? _domainSetting.Local : _domainSetting.Server;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                SetAuthorizationHeader();

                if (!isAuthen)
                {
                    TempData["ErrorMessage"] = "Bạn chưa đăng nhập";
                    return RedirectToPage("/Login");
                }

                if (!Roles.Contains("PHARMACY"))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này";
                    return RedirectToPage("/OrderPage/MyOrders");
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định người dùng";
                    return RedirectToPage("/Login");
                }

                Orders = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.IsDeleted == false && o.UserId == userId)
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToPage("/Login");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                SetAuthorizationHeader();

                if (!isAuthen || !Roles.Contains("PHARMACY"))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này";
                    return RedirectToPage("/OrderPage/MyOrders");
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định người dùng";
                    return RedirectToPage("/Login");
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xóa đơn hàng này";
                    return RedirectToPage();
                }

                using (var client = _httpClientFactory.CreateClient())
                {
                    var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    string _url = $"{GetCurrentDomain()}order";
                    var response = await client.DeleteAsync($"{_url}/{id}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<BaseResponse<Order>>(responseContent);

                        if (result.Success)
                        {
                            TempData["SuccessMessage"] = result.Message ?? "Xóa đơn hàng thành công";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = result.Message ?? "Lỗi khi xóa đơn hàng";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}