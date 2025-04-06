using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.ShipmentPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class DetailsModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:5273";
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(HttpClient httpClient, ILogger<DetailsModel> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ShipmentReponseModel Shipment { get; set; } = default!;

        // Dictionary để ánh xạ Id thành Name
        public Dictionary<string, string> AreaNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SupplierNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> MedicalNames { get; set; } = new Dictionary<string, string>();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Không tìm thấy access token trong session khi tải chi tiết lô hàng.");
                    ModelState.AddModelError("", "Bạn cần đăng nhập để xem chi tiết lô hàng.");
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/shipment/{id}");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "*/*");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(content);

                    if (responseObject.Success && responseObject.Result != null)
                    {
                        Shipment = responseObject.Result;
                        // Ghi log để kiểm tra giá trị AreaId và SupplierId
                        _logger.LogInformation("Shipment AreaId: {AreaId}", Shipment.AreaId);
                        _logger.LogInformation("Shipment SupplierId: {SupplierId}", Shipment.SupplierId);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    return NotFound();
                }

                // Tải dữ liệu và kiểm tra dictionary
                await LoadAreasAsync();
                await LoadSuppliersAsync();
                await LoadMedicalsAsync();

                // Kiểm tra và ghi log số lượng phần tử trong dictionary
                _logger.LogInformation("AreaNames count: {Count}", AreaNames.Count);
                _logger.LogInformation("SupplierNames count: {Count}", SupplierNames.Count);

                // Kiểm tra xem AreaId và SupplierId có trong dictionary không
                if (!AreaNames.ContainsKey(Shipment.AreaId.ToString()))
                {
                    _logger.LogWarning("AreaId {AreaId} không tồn tại trong AreaNames", Shipment.AreaId);
                }
                if (!SupplierNames.ContainsKey(Shipment.SupplierId.ToString()))
                {
                    _logger.LogWarning("SupplierId {SupplierId} không tồn tại trong SupplierNames", Shipment.SupplierId);
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("Lỗi yêu cầu HTTP: {Message}", e.Message);
                return StatusCode(500);
            }

            return Page();
        }

        private async Task LoadAreasAsync()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/area");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                var response = await _httpClient.SendAsync(request);
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
                else
                {
                    _logger.LogWarning("Không tải được danh sách khu vực, mã trạng thái: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi tải danh sách khu vực: {Message}", ex.Message);
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/supplier");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                var response = await _httpClient.SendAsync(request);
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
                else
                {
                    _logger.LogWarning("Không tải được danh sách nhà cung cấp, mã trạng thái: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi tải danh sách nhà cung cấp: {Message}", ex.Message);
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
     
    }
}