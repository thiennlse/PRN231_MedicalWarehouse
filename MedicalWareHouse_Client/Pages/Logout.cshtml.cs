using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MedicalWareHouse_Client.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            // Đăng xuất xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Xóa toàn bộ session của user
            HttpContext.Session.Clear(); 
            
            // Xóa cookie (nếu bạn sử dụng cookie để xác thực)
            foreach (var cookie in HttpContext.Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return RedirectToPage("/Login"); // Chuyển hướng về trang Login
        }
    }
}
