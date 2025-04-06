using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.ShipmentPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class CreateModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<CreateModel> _logger;
        private readonly string _baseUrl = "http://localhost:5273";

        public CreateModel(HttpClient httpClient, IHttpContextAccessor httpContext, ILogger<CreateModel> logger)
        {
            _httpClient = httpClient;
            _httpContext = httpContext;
            _logger = logger;
        }

        [BindProperty]
        public Guid? OrderId { get; set; }

        public List<OrderDetailViewModel> OrderDetails { get; set; } = new List<OrderDetailViewModel>();
        public SelectList AreaOptions { get; set; }
        public SelectList SupplierOptions { get; set; }

        public class OrderDetailViewModel
        {
            public Guid MedicalId { get; set; }
            public string MedicalName { get; set; }
            public int OrderQuantity { get; set; }
            public DateTime ExpiredDate { get; set; }
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("No AccessToken found in session");
            }
        }

        public async Task<IActionResult> OnGetAsync(Guid? orderId)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("No OrderId provided");
                return RedirectToPage("/OrderPage/Index");
            }

            OrderId = orderId;
            _httpContext.HttpContext.Session.SetString("CurrentOrderId", orderId.ToString());
            _logger.LogInformation("Processing OrderId: {OrderId}", orderId);

            SetAuthorizationHeader();

            // Load order details
            var response = await _httpClient.GetAsync($"{_baseUrl}/order/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<BaseResponse<OrderResponseModel>>(content);

                if (orderResponse.Success && orderResponse.Result != null)
                {
                    OrderDetails = new List<OrderDetailViewModel>();

                    foreach (var od in orderResponse.Result.OrderDetails)
                    {
                        // Gọi API để lấy thông tin Medical theo MedicalId
                        var medicalResponse = await _httpClient.GetAsync($"{_baseUrl}/medical/{od.MedicalId}");
                        string medicalName = "Unknown Medical"; // Giá trị mặc định nếu không lấy được tên

                        if (medicalResponse.IsSuccessStatusCode)
                        {
                            var medicalContent = await medicalResponse.Content.ReadAsStringAsync();
                            var medicalData = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(medicalContent);
                            if (medicalData.Success && medicalData.Result != null)
                            {
                                medicalName = medicalData.Result.Name; // Giả sử API trả về trường "Name"
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch medical details for MedicalId: {MedicalId}", od.MedicalId);
                        }

                        OrderDetails.Add(new OrderDetailViewModel
                        {
                            MedicalId = od.MedicalId,
                            MedicalName = medicalName,
                            OrderQuantity = od.OrderQuantity,
                            ExpiredDate = od.ExpiredDate
                        });
                    }
                    _logger.LogInformation("Loaded {Count} order details", OrderDetails.Count);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch order details for OrderId: {OrderId}. Status: {Status}, Response: {Response}", orderId, response.StatusCode, errorContent);
                TempData["ErrorMessage"] = "Không thể tải thông tin đơn hàng.";
            }

            // Load areas and suppliers
            await LoadDropdowns();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid areaId, Guid supplierId, DateTime shipDate, List<ShipmentDetailModel> shipmentDetails)
        {
            SetAuthorizationHeader();
            var orderId = _httpContext.HttpContext.Session.GetString("CurrentOrderId");

            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("OrderId is null or empty in session");
                TempData["ErrorMessage"] = "Không thể tạo lô hàng: Không tìm thấy OrderId trong phiên.";
                await LoadDropdowns();
                return Page();
            }

            // Create the shipment
            var shipmentRequest = new
            {
                areaId,
                supplierId,
                shipDate,
                shipmentDetails,
                orderId = Guid.Parse(orderId)
            };

            _logger.LogInformation("Creating shipment for OrderId: {OrderId}", orderId);
            _logger.LogInformation("Shipment request payload: {Payload}", JsonConvert.SerializeObject(shipmentRequest));
            var content = new StringContent(JsonConvert.SerializeObject(shipmentRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/shipment/create", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create shipment. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);
                TempData["ErrorMessage"] = $"Không thể tạo lô hàng: {errorContent}";
                await LoadDropdowns();
                return Page(); // Stay on the Create page
            }

            _logger.LogInformation("Shipment created successfully");
            
            // No need to update order status here as it's now handled by the server
            
            TempData["CreateShipmentSuccessMessage"] = "Tạo lô hàng thành công và trạng thái đơn hàng đã được cập nhật";
            return RedirectToPage("/OrderPage/Index"); // Redirect to Order Index page on success
        }

        private async Task LoadDropdowns()
        {
            // Load Areas
            var areaResponse = await _httpClient.GetAsync($"{_baseUrl}/area");
            if (areaResponse.IsSuccessStatusCode)
            {
                var areas = JsonConvert.DeserializeObject<BaseResponse<AreaResponseModel>>(
                    await areaResponse.Content.ReadAsStringAsync());
                AreaOptions = new SelectList(areas.Results, "AreaId", "AreaName");
            }
            else
            {
                _logger.LogError("Failed to load areas. Status: {Status}", areaResponse.StatusCode);
            }

            // Load Suppliers
            var supplierResponse = await _httpClient.GetAsync($"{_baseUrl}/supplier");
            if (supplierResponse.IsSuccessStatusCode)
            {
                var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(
                    await supplierResponse.Content.ReadAsStringAsync());
                SupplierOptions = new SelectList(suppliers, "Id", "Name");
            }
            else
            {
                _logger.LogError("Failed to load suppliers. Status: {Status}", supplierResponse.StatusCode);
            }
        }
    }

    public class ShipmentDetailModel
    {
        public Guid MedicalId { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}