using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Http;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.ShipmentPage
{
    [RequireAuthentication("ADMIN", "STAFF")]
    public class DeleteModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:5273"; // Thay đổi nếu cần

        public DeleteModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [BindProperty]
        public ShipmentReponseModel Shipment { get; set; } = default!;

        // Dictionary để ánh xạ Id thành Name
        public Dictionary<string, string> AreaNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SupplierNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> MedicalNames { get; set; } = new Dictionary<string, string>();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            try
            {
                // Thiết lập header Authorization nếu cần
                SetAuthorizationHeader();

                // Tải dữ liệu lô hàng
                var response = await _httpClient.GetAsync($"{_baseUrl}/shipment/{id}");
                if (!response.IsSuccessStatusCode) return NotFound();

                var shipmentData = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(shipmentData);

                if (apiResponse == null || !apiResponse.Success || apiResponse.Result == null) return NotFound();

                Shipment = apiResponse.Result;

                // Tải danh sách Areas, Suppliers, và Medicals để ánh xạ Name
                await LoadAreasAsync();
                await LoadSuppliersAsync();
                await LoadMedicalsAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Lỗi yêu cầu HTTP: {e.Message}");
                return StatusCode(500);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var accessToken = HttpContext.Session.GetString("AccessToken");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(accessToken))
            {
                ModelState.AddModelError(string.Empty, "Không được phép. Vui lòng đăng nhập lại.");
                return RedirectToPage("/Login");
            }

            if (userRole != "STAFF" && userRole != "ADMIN")
            {
                ModelState.AddModelError(string.Empty, "Bạn không có quyền xóa lô hàng.");
                TempData["ErrorMessage"] = "Bạn không có quyền xóa lô hàng.";
                return Page();
            }

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/shipment/{id}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xóa lô hàng thành công.";
                return RedirectToPage("/ShipmentPage/Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Không thể xóa lô hàng.");
                TempData["ErrorMessage"] = "Không thể xóa lô hàng.";
                return Page();
            }
        }

        private void SetAuthorizationHeader()
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task LoadAreasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/area");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<BaseResponse<AreaResponseModel>>(content);
                    if (apiResponse != null && apiResponse.Success && apiResponse.Results != null)
                    {
                        AreaNames = apiResponse.Results.ToDictionary(
                            a => a.AreaId.ToString(),
                            a => a.AreaName
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tải danh sách khu vực: {ex.Message}");
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/supplier");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);
                    if (suppliers != null && suppliers.Count > 0)
                    {
                        SupplierNames = suppliers.ToDictionary(
                            s => s.Id.ToString(),
                            s => s.Name
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tải danh sách nhà cung cấp: {ex.Message}");
            }
        }

        private async Task LoadMedicalsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/medical/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);
                    if (apiResponse != null && apiResponse.Success && apiResponse.Results != null)
                    {
                        MedicalNames = apiResponse.Results.ToDictionary(
                            m => m.Id.ToString(),
                            m => m.Name
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tải danh sách sản phẩm y tế: {ex.Message}");
            }
        }

        public class ApiResponse
        {
            public bool Success { get; set; }
            public ShipmentReponseModel Result { get; set; }
        }

     
    }
}