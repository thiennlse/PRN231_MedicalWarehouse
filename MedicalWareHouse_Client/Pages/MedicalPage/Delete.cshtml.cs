using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;

namespace MedicalWareHouse_Client.Pages.MedicalPage
{
    public class DeleteModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContext,
            ILogger<DeleteModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
            _logger = logger;
        }

        [BindProperty]
        public MedicalResponseModel Medical { get; set; }

        private void SetAuthorizationHeader(HttpClient client)
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private string GetCurrentDomain() => _domainSetting.Local;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue) return NotFound();

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);
                var apiUrl = $"{GetCurrentDomain()}medical/{id}";

                _logger.LogInformation("Fetching medical item for deletion: {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);

                if (result?.Success != true || result.Result == null)
                {
                    _logger.LogWarning("Medical item not found for deletion: {Id}", id);
                    return NotFound();
                }

                Medical = result.Result;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading medical item for deletion with ID {MedicalId}", id);
                ModelState.AddModelError("", "Lỗi khi tải thông tin sản phẩm: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (!id.HasValue) return NotFound();

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);

                var apiUrl = $"{GetCurrentDomain()}medical/delete/{id}";

                _logger.LogInformation("Soft deleting medical item: {ApiUrl}", apiUrl);

                var response = await client.DeleteAsync(apiUrl);
                var resultContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(resultContent);

                if (result?.Success == true)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Xóa thành công";
                    return RedirectToPage("./Index");
                }
                else
                {
                    _logger.LogWarning("Soft delete failed: {Response}", resultContent);
                    ModelState.AddModelError("", result?.Message ?? "Lỗi khi xóa sản phẩm");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting medical item with ID {MedicalId}", id);
                ModelState.AddModelError("", "Lỗi khi xóa sản phẩm: " + ex.Message);
                return Page();
            }
        }
    }
}