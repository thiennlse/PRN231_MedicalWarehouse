using CloudinaryDotNet.Actions;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.SupplierPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly DomainSetting _domainSetting;
        private readonly IWebHostEnvironment _env;

        public IndexModel(HttpClient httpClient, IHttpContextAccessor httpContext, IOptions<DomainSetting> option, IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContext = httpContext;
            _env = env;
            _domainSetting = option.Value;
        }

        public List<SupplierResponse> Suppliers = new List<SupplierResponse>();
        public bool isAuthen { get; set; } = false;
        public List<string> Roles { get; set; } = new List<string>();
        
        [BindProperty]
        public SupplierRequestModelWithValidation CSupplier { get; set; }
        
        [BindProperty]
        public string? KeySearch { get; set; } = "";
        [BindProperty]
        public int CurrentPage { get; set; } = 1;
        [BindProperty]
        public int PageSize { get; set; } = 5;
        [BindProperty]
        public bool hasPrePage { get; set; } = false;
        [BindProperty]
        public bool hasNextPage { get; set; } = true;

        // Custom validation model class
        public class SupplierRequestModelWithValidation
        {
            [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số và chỉ chứa các số từ 0-9")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Email là bắt buộc")]
            [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Email phải có định dạng hợp lệ, chứa ký tự @ và không chứa khoảng trắng")]
            public string ContactEmail { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? KeySearch, int? CurrentPage, int? PageSize)
        {
            try
            {
                SetAuthorizationHeader();
                Roles = GetUserRoles();
                string _url = $"{GetCurrentDomain()}odata/supplier?";

                if (!string.IsNullOrEmpty(KeySearch))
                {
                    this.KeySearch = KeySearch;
                    _url = $"{_url}$filter=contains(Name,'{KeySearch}') or contains(ContactEmail,'{KeySearch}')&";
                }
                this.CurrentPage = CurrentPage ?? this.CurrentPage;
                this.PageSize = PageSize ?? this.PageSize;

                var response = await _httpClient.GetAsync($"{_url}$top={this.PageSize}&$skip={(this.CurrentPage - 1) * this.PageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);
                    if (responseObject != null)
                    {
                        Suppliers = responseObject;
                        hasPrePage = this.CurrentPage > 1;
                        hasNextPage = responseObject.Count() == this.PageSize;
                    }
                    else
                    {
                        Console.WriteLine("Phản hồi API không thành công");
                        hasPrePage = false;
                        hasNextPage = false;
                    }
                }
            }
            catch (HttpRequestException e)
            {
                ModelState.AddModelError("", $"Lỗi khi tải danh sách nhà cung cấp: {e.Message}");
                hasPrePage = false;
                hasNextPage = false;
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                ModelState.AddModelError("", $"Lỗi phân tích dữ liệu JSON: {e.Message}");
                hasPrePage = false;
                hasNextPage = false;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Validate supplier data
            if (!ValidateSupplierData())
            {
                TempData["FormWithErrors"] = "addSupplierModal";
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }

            try
            {
                SetAuthorizationHeader();
                string _url = $"{GetCurrentDomain()}supplier";
                
                // Convert to original SupplierRequestModel
                var supplierRequest = new SupplierRequestModel
                {
                    Name = CSupplier.Name,
                    PhoneNumber = CSupplier.PhoneNumber,
                    ContactEmail = CSupplier.ContactEmail
                };

                var json = JsonConvert.SerializeObject(supplierRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_url}/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<SupplierResponse>>(responseContent);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Thêm nhà cung cấp thành công";
                    return RedirectToPage();
                }
                else
                {
                    TempData["FormWithErrors"] = "addSupplierModal";
                    ModelState.AddModelError("", result.Message ?? "Lỗi khi thêm nhà cung cấp");
                }
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
            catch (HttpRequestException e)
            {
                ModelState.AddModelError("", $"Lỗi khi thêm nhà cung cấp: {e.Message}");
                TempData["FormWithErrors"] = "addSupplierModal";
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(Guid SupplierId)
        {
            // Validate supplier data
            if (!ValidateSupplierData())
            {
                TempData["FormWithErrors"] = "updateSupplierModal";
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
            
            try
            {
                SetAuthorizationHeader();
                string _url = $"{GetCurrentDomain()}supplier";
                
                // Convert to original SupplierRequestModel
                var supplierRequest = new SupplierRequestModel
                {
                    Name = CSupplier.Name,
                    PhoneNumber = CSupplier.PhoneNumber,
                    ContactEmail = CSupplier.ContactEmail
                };
                
                var json = JsonConvert.SerializeObject(supplierRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_url}/{SupplierId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<BaseResponse<SupplierResponse>>(responseContent);

                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = result.Message ?? "Cập nhật nhà cung cấp thành công";
                        return RedirectToPage();
                    }
                    else
                    {
                        TempData["FormWithErrors"] = "updateSupplierModal";
                        ModelState.AddModelError("", result.Message ?? "Lỗi khi cập nhật nhà cung cấp");
                    }
                }
                else
                {
                    TempData["FormWithErrors"] = "updateSupplierModal";
                    ModelState.AddModelError("", $"Lỗi API: {response.StatusCode}");
                }
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
            catch (HttpRequestException e)
            {
                TempData["FormWithErrors"] = "updateSupplierModal";
                ModelState.AddModelError("", $"Lỗi khi cập nhật nhà cung cấp: {e.Message}");
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid SupplierId)
        {
            try
            {
                SetAuthorizationHeader();
                string _url = $"{GetCurrentDomain()}supplier";
                
                // Kiểm tra xem nhà cung cấp có lô hàng không bằng cách gọi API
                var checkResponse = await _httpClient.GetAsync($"{_url}/{SupplierId}");
                if (checkResponse.IsSuccessStatusCode)
                {
                    var content = await checkResponse.Content.ReadAsStringAsync();
                    var supplierData = JsonConvert.DeserializeObject<SupplierResponse>(content);
                    
                    if (supplierData != null && supplierData.Shipment != null && supplierData.Shipment.Any())
                    {
                        TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này vì đã có hợp tác trong các lô hàng.";
                        return RedirectToPage();
                    }
                }
                
                var response = await _httpClient.DeleteAsync($"{_url}/{SupplierId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<BaseResponse<SupplierResponse>>(responseContent);

                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = result.Message ?? "Xóa nhà cung cấp thành công";
                        return RedirectToPage();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message ?? "Lỗi khi xóa nhà cung cấp";
                    }
                }
                else
                {
                    // Xử lý lỗi BadRequest hoặc các lỗi khác
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        if (responseContent.Contains("shipment") || responseContent.Contains("lô hàng") || 
                            responseContent.ToLower().Contains("đã có") || responseContent.ToLower().Contains("cannot delete"))
                        {
                            TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này vì đã có hợp tác trong các lô hàng.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này. Vui lòng thử lại sau.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode}";
                    }
                }
                return RedirectToPage();
            }
            catch (HttpRequestException e)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa nhà cung cấp: {e.Message}";
                await OnGetAsync(KeySearch, CurrentPage, PageSize);
                return Page();
            }
        }

        private bool ValidateSupplierData()
        {
            // Validate supplier name
            if (string.IsNullOrWhiteSpace(CSupplier.Name))
            {
                ModelState.AddModelError("CSupplier.Name", "Tên nhà cung cấp là bắt buộc");
                return false;
            }

            // Validate phone number (exactly 10 digits, numbers only, no spaces)
            if (string.IsNullOrWhiteSpace(CSupplier.PhoneNumber))
            {
                ModelState.AddModelError("CSupplier.PhoneNumber", "Số điện thoại là bắt buộc");
                return false;
            }
            
            // Remove any potential whitespace in phone number
            CSupplier.PhoneNumber = CSupplier.PhoneNumber.Replace(" ", "");
            
            if (!Regex.IsMatch(CSupplier.PhoneNumber, @"^[0-9]{10}$"))
            {
                ModelState.AddModelError("CSupplier.PhoneNumber", "Số điện thoại phải có đúng 10 chữ số và chỉ chứa các số từ 0-9");
                return false;
            }

            // Validate email (must contain @ and no spaces)
            if (string.IsNullOrWhiteSpace(CSupplier.ContactEmail))
            {
                ModelState.AddModelError("CSupplier.ContactEmail", "Email là bắt buộc");
                return false;
            }

            // Check for spaces in email
            if (CSupplier.ContactEmail.Contains(" "))
            {
                ModelState.AddModelError("CSupplier.ContactEmail", "Email không được chứa khoảng trắng");
                return false;
            }

            if (!CSupplier.ContactEmail.Contains("@"))
            {
                ModelState.AddModelError("CSupplier.ContactEmail", "Email phải chứa ký tự @");
                return false;
            }

            return ModelState.IsValid;
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
        }

        private string GetUserName()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            return nameClaim?.Value;
        }

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

        public string GetCurrentDomain()
        {
            return _env.IsDevelopment() ? _domainSetting.Local : _domainSetting.Server;
        }
    }
}