using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Text;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.AreaPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseurl = "http://localhost:5273";
        public List<AreaViewModel> AreaList { get; set; } = new List<AreaViewModel>();

        [BindProperty]
        public Area Area { get; set; } = default!;

        // Search parameters
        [BindProperty(SupportsGet = true)]
        public string SearchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }

        public IndexModel(IHttpContextAccessor httpContext)
        {
            _httpClient = new HttpClient();
            _httpContext = httpContext;
        }

        public async Task OnGetAsync()
        {
            AreaList = new List<AreaViewModel>();

            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{_baseurl}/area");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<BaseResponse<AreaResponseModel>>(content);
                    if (responseObject.Success)
                    {
                        if (responseObject.Results.Any())
                        {
                            foreach (var area in responseObject.Results)
                            {
                                var areaViewModel = new AreaViewModel
                                {
                                    Id = area.AreaId,
                                    Name = area.AreaName,
                                    Shipments = area.Shipment?.Select(s => new ShipmentViewModel
                                    {
                                        Id = s.ShipmentId,
                                        Name = s.Name,
                                        ShipDate = s.ShipDate,
                                        ShipmentDetails = s.ShipmentDetails
                                    }).ToList() ?? new List<ShipmentViewModel>()
                                };
                                AreaList.Add(areaViewModel);
                            }

                            // Apply search filters
                            if (!string.IsNullOrEmpty(SearchId))
                            {
                                if (Guid.TryParse(SearchId, out Guid id))
                                {
                                    AreaList = AreaList.Where(a => a.Id.Equals(id)).ToList();
                                }
                            }

                            if (!string.IsNullOrEmpty(SearchName))
                            {
                                AreaList = AreaList.Where(a => a.Name.ToLower().Contains(SearchName.ToLower())).ToList();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Phản hồi API không thành công. Thông báo: {responseObject.Message}");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Lỗi yêu cầu HTTP: {e.Message}");
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Console.WriteLine($"Lỗi phân tích JSON: {e.Message}");
            }
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            try
            {
                var areaName = new AreaRequestModel
                {
                    AreaName = Area.Name
                };
                var json = System.Text.Json.JsonSerializer.Serialize(areaName);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                SetAuthorizationHeader();
                var response = await _httpClient.PostAsync($"{_baseurl}/area/create", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Thêm khu vực thành công!";
                    return RedirectToPage();
                }

                TempData["ErrorMessage"] = "Thêm khu vực thất bại!";
                return RedirectToPage();
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Console.WriteLine($"Lỗi phân tích JSON: {e.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thêm khu vực!";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                var areaId = Area.Id;
                var areaName = new AreaRequestModel
                {
                    AreaName = Area.Name
                };
                var json = System.Text.Json.JsonSerializer.Serialize(areaName);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                SetAuthorizationHeader();
                var response = await _httpClient.PutAsync($"{_baseurl}/area/{areaId}", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật khu vực thành công!";
                    return RedirectToPage();
                }
                TempData["ErrorMessage"] = "Cập nhật khu vực thất bại!";
                return RedirectToPage();
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Console.WriteLine($"Lỗi: {e.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật khu vực!";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetDeleteAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out Guid areaId))
                {
                    TempData["ErrorMessage"] = "ID khu vực không hợp lệ!";
                    return RedirectToPage();
                }

                SetAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"{_baseurl}/area/{areaId}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xóa khu vực thành công!";
                    return RedirectToPage();
                }
                TempData["ErrorMessage"] = "Xóa khu vực thất bại!";
                return RedirectToPage();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Lỗi yêu cầu HTTP: {e.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa khu vực!";
                return RedirectToPage();
            }
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    public class AreaViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ShipmentViewModel> Shipments { get; set; } = new List<ShipmentViewModel>();
    }

    public class ShipmentViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime ShipDate { get; set; }
        public List<ShipmentDetailResponse> ShipmentDetails { get; set; } = new List<ShipmentDetailResponse>();
        
        public int TotalItems => ShipmentDetails?.Sum(sd => sd.Quantity) ?? 0;
        public int UniqueItems => ShipmentDetails?.Count ?? 0;
    }
}