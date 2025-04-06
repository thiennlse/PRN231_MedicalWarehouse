using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using MedicalWarehouse_BusinessObject.Enums;
using System.Net.Http.Headers;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.OrderPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IWebHostEnvironment _env;

        public EditModel(
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContext,
            IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
            _env = env;
        }

        [BindProperty]
        public OrderRequestModel OrderRequest { get; set; } = new();

        public OrderResponseModel OrderResponse { get; set; }

        public List<SelectListItem> MedicalItems { get; set; } = new();

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

        private async Task LoadMedicalItems()
        {
            using var client = _httpClientFactory.CreateClient();
            SetAuthorizationHeader(client);
            var apiUrl = GetCurrentDomain() + "medical";
            var response = await client.GetAsync(apiUrl);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);

            if (result?.Results != null)
            {
                MedicalItems = result.Results.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.Name} (Giá: {m.Price})"
                }).ToList();
            }
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            await LoadMedicalItems();

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);
                var apiUrl = GetCurrentDomain() + $"order/{id}";
                var response = await client.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<OrderResponseModel>>(content);

                if (!result.Success || result.Result == null)
                {
                    return NotFound();
                }

                OrderResponse = result.Result;

                // Map response to request model
                OrderRequest = new OrderRequestModel
                {
                    Type = OrderResponse.Type,
                    Status = OrderResponse.Status,
                    OrderDetail = OrderResponse.OrderDetails.Select(od => new OrderDetailRequestModel
                    {
                        MedicalId = od.MedicalId,
                        Quantity = od.OrderQuantity
                    }).ToList()
                };

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi tải đơn hàng: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                await LoadMedicalItems();
                return Page();
            }

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);
                var apiUrl = GetCurrentDomain() + $"order/{id}";

                var json = JsonConvert.SerializeObject(OrderRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<OrderResponseModel>>(responseContent);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Cập nhật đơn hàng thành công";
                    return RedirectToPage("./Index");
                }
                else
                {
                    ModelState.AddModelError("", result.Message ?? "Lỗi khi cập nhật đơn hàng");
                    await LoadMedicalItems();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật đơn hàng: " + ex.Message);
                await LoadMedicalItems();
                return Page();
            }
        }
    }
}