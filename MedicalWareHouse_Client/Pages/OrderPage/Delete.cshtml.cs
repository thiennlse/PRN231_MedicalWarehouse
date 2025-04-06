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

namespace MedicalWareHouse_Client.Pages.OrderPage
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IWebHostEnvironment _env;

        public DeleteModel(
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

        [BindProperty]
        public Order Order { get; set; } = default!;

        private void SetAuthorizationHeader(HttpClient client)
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }
            
            Order = order;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    SetAuthorizationHeader(client);
                    string _url = $"{GetCurrentDomain()}order";
                    var response = await client.DeleteAsync($"{_url}/{id}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<BaseResponse<Order>>(responseContent);

                        if (result.Success)
                        {
                            TempData["SuccessMessage"] = result.Message ?? "Xóa đơn hàng thành công";
                            return RedirectToPage("./Index");
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
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }

        private string GetCurrentDomain()
        {
            return _env.IsDevelopment() ? _domainSetting.Local : _domainSetting.Server;
        }
    }
}