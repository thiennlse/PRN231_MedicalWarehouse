using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Enums;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.Extensions.Options;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.OrderPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IWebHostEnvironment _env;

        public CreateModel(
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
        public OrderRequestModel OrderRequest { get; set; } = new();

        private List<string> GetUserRoles()
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token)) return new List<string>();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();
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

        private async Task LoadMedicalItems()
        {
            var medicalItems = await _context.Medicals
                .AsNoTracking()
                .Where(m => m.IsDeleted == false)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync();
            ViewData["MedicalItems"] = new SelectList(medicalItems, "Id", "Name");
        }

        private string GetCurrentDomain()
        {
            return _env.IsDevelopment() ? _domainSetting.Local : _domainSetting.Server;
        }

        public async Task<IActionResult> OnGet([FromQuery] OrderType type, [FromQuery] string cartData)
        {
            try
            {
                var roles = GetUserRoles();

                if (type == OrderType.Import)
                {
                    OrderRequest.Type = type;
                    await LoadMedicalItems();
                    return Page();
                }

                if (type == OrderType.Export)
                {
                    if (!roles.Contains("PHARMACY"))
                    {
                        return RedirectToPage("./MyOrders");
                    }
                    
                    OrderRequest.Type = type;
                    
                    // Xử lý dữ liệu giỏ hàng nếu có
                    if (!string.IsNullOrEmpty(cartData))
                    {
                        try
                        {
                            var cartItems = JsonConvert.DeserializeObject<List<CartItemDto>>(cartData);
                            if (cartItems != null && cartItems.Any())
                            {
                                OrderRequest.OrderDetail = cartItems.Select(item => new OrderDetailRequestModel
                                {
                                    MedicalId = Guid.Parse(item.MedicalId),
                                    Quantity = item.Quantity
                                }).ToList();
                                
                                Console.WriteLine($"Loaded {OrderRequest.OrderDetail.Count} items from cart data");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing cart data: {ex.Message}");
                            ModelState.AddModelError(string.Empty, "Không thể đọc dữ liệu giỏ hàng");
                        }
                    }
                    
                    await LoadMedicalItems();
                    return Page();
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi khi tải danh sách sản phẩm y tế: {ex.Message}");
                await LoadMedicalItems();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Log incoming request for debugging
                LogOrderRequest();

                if (!ModelState.IsValid)
                {
                    LogModelStateErrors();
                    await LoadMedicalItems();
                    return Page();
                }

                if (OrderRequest.OrderDetail == null || !OrderRequest.OrderDetail.Any())
                {
                    ModelState.AddModelError(string.Empty, "Cần ít nhất một chi tiết đơn hàng");
                    await LoadMedicalItems();
                    return Page();
                }

                // Log chi tiết dữ liệu nhận được
                Console.WriteLine("Received Order Details:");
                foreach (var detail in OrderRequest.OrderDetail)
                {
                    Console.WriteLine($"MedicalId: {detail.MedicalId}, Quantity: {detail.Quantity}");
                }

                // Validate each order detail
                var validOrderDetails = new List<OrderDetailRequestModel>();
                foreach (var detail in OrderRequest.OrderDetail)
                {
                    if (detail == null || detail.MedicalId == Guid.Empty || detail.Quantity <= 0)
                    {
                        Console.WriteLine($"Invalid detail skipped: MedicalId={detail?.MedicalId}, Quantity={detail?.Quantity}");
                        continue;
                    }

                    var medical = await _context.Medicals
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == detail.MedicalId);
                    if (medical == null)
                    {
                        ModelState.AddModelError(string.Empty, $"Sản phẩm y tế với ID {detail.MedicalId} không được tìm thấy");
                        await LoadMedicalItems();
                        return Page();
                    }

                    validOrderDetails.Add(detail);
                }

                if (!validOrderDetails.Any())
                {
                    ModelState.AddModelError(string.Empty, "Không có chi tiết đơn hàng hợp lệ");
                    await LoadMedicalItems();
                    return Page();
                }

                OrderRequest.OrderDetail = validOrderDetails;

                // Set default status if not provided
                OrderRequest.Status = OrderRequest.Status == 0 ? OrderStatus.ORDERED : OrderRequest.Status;

                // Call API to create order
                using var client = _httpClientFactory.CreateClient();
                SetAuthorizationHeader(client);
                var apiUrl = GetCurrentDomain() + "order/create";

                var requestObj = new
                {
                    type = (int)OrderRequest.Type,
                    status = (int)OrderRequest.Status,
                    orderDetails = OrderRequest.OrderDetail.Select(d => new
                    {
                        medicalId = d.MedicalId,
                        quantity = d.Quantity
                    }).ToList()
                };

                var requestJson = JsonConvert.SerializeObject(requestObj, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
                Console.WriteLine($"Dữ liệu yêu cầu gửi API: {requestJson}");

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Trạng thái phản hồi: {response.StatusCode}");
                Console.WriteLine($"Dữ liệu phản hồi: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi API: {response.StatusCode} - {responseContent}");
                    await LoadMedicalItems();
                    return Page();
                }

                var result = JsonConvert.DeserializeObject<BaseResponse<OrderResponseModel>>(responseContent);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message ?? "Lỗi khi tạo đơn hàng");
                    await LoadMedicalItems();
                    return Page();
                }

                // Đơn hàng đã được tạo thành công, nếu là đơn xuất từ giỏ hàng (PHARMACY) thì xóa giỏ hàng
                if (OrderRequest.Type == OrderType.Export && GetUserRoles().Contains("PHARMACY"))
                {
                    try
                    {
                        // Xóa giỏ hàng trên server
                        using var cartClient = _httpClientFactory.CreateClient();
                        SetAuthorizationHeader(cartClient);
                        var cartClearResponse = await cartClient.DeleteAsync(GetCurrentDomain() + "api/cart/clear");
                        
                        // Thêm script để xóa giỏ hàng trong localStorage ở client
                        TempData["ClearCart"] = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Không thể xóa giỏ hàng: {ex.Message}");
                        // Tiếp tục quy trình mặc dù xóa giỏ hàng thất bại
                    }
                }

                TempData["SuccessMessage"] = result.Message ?? "Đơn hàng đã được tạo thành công";
                var roles = GetUserRoles();
                return OrderRequest.Type == OrderType.Export && roles.Contains("PHARMACY")
                    ? RedirectToPage("./MyOrders")
                    : RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi: {ex.Message}");
                await LoadMedicalItems();
                return Page();
            }
        }

        private void LogOrderRequest()
        {
            Console.WriteLine($"OrderRequest.Type: {OrderRequest.Type}");
            Console.WriteLine($"OrderRequest.Status: {OrderRequest.Status}");
            Console.WriteLine($"OrderRequest.OrderDetail Count: {OrderRequest.OrderDetail?.Count ?? 0}");
            if (OrderRequest.OrderDetail != null)
            {
                for (int i = 0; i < OrderRequest.OrderDetail.Count; i++)
                {
                    var detail = OrderRequest.OrderDetail[i];
                    Console.WriteLine($"Detail [{i}]: MedicalId={detail.MedicalId}, Quantity={detail.Quantity}");
                }
            }
        }

        private void LogModelStateErrors()
        {
            foreach (var modelStateKey in ModelState.Keys)
            {
                var modelStateVal = ModelState[modelStateKey];
                foreach (var error in modelStateVal.Errors)
                {
                    Console.WriteLine($"Key: {modelStateKey}, Error: {error.ErrorMessage}");
                }
            }
        }

        // Class để deserialize dữ liệu giỏ hàng
        private class CartItemDto
        {
            public string MedicalId { get; set; }
            public int Quantity { get; set; }
        }
    }
}