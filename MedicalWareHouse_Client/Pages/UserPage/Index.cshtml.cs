using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Http;
using System.Linq;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.UserPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class IndexModel : PageModel
    {
        public List<UserResponseModel> Users { get; set; } = new List<UserResponseModel>();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public string KeySearch { get; set; }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1, string keySearch = "")
        {
            var accessToken = _httpContextAccessor.HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/Login");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("http://localhost:5273/auth/user");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserResponseModel>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Lấy toàn bộ danh sách người dùng từ API
                var allUsers = apiResponse.Results;

                // Lọc theo KeySearch (phía client-side)
                KeySearch = keySearch;
                if (!string.IsNullOrEmpty(KeySearch))
                {
                    allUsers = allUsers.Where(u =>
                        u.Name.Contains(KeySearch, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(KeySearch, StringComparison.OrdinalIgnoreCase) ||
                        u.Role.Contains(KeySearch, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Tính toán phân trang
                CurrentPage = pageNumber;
                TotalPages = (int)Math.Ceiling(allUsers.Count / (double)PageSize);
                Users = allUsers.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            }
            else
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var accessToken = _httpContextAccessor.HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/Login");
            }

            // Lấy thông tin user hiện tại từ session hoặc token
            var currentUserId = _httpContextAccessor.HttpContext.Session.GetString("UserId"); // Giả sử bạn lưu UserId trong session khi đăng nhập

            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                return RedirectToPage(new { KeySearch = KeySearch, CurrentPage = CurrentPage, PageSize = PageSize });
            }

            // Kiểm tra nếu ID của user cần xóa trùng với ID của user hiện tại
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa chính mình!";
                return RedirectToPage(new { KeySearch = KeySearch, CurrentPage = CurrentPage, PageSize = PageSize });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.DeleteAsync($"http://localhost:5273/auth/user/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xóa người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Xóa người dùng thất bại.";
            }

            return RedirectToPage(new { KeySearch = KeySearch, CurrentPage = CurrentPage, PageSize = PageSize });
        }

        public async Task<IActionResult> OnPostUpdateAsync(string id)
        {
            var accessToken = _httpContextAccessor.HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/Login");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsync($"http://localhost:5273/auth/make_staff?userId={id}", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Cập nhật người dùng thất bại.";
            }

            return RedirectToPage(new { KeySearch = KeySearch, CurrentPage = CurrentPage, PageSize = PageSize });
        }
    }
}