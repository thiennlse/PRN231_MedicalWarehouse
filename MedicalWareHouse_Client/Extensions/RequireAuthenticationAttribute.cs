using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicalWareHouse_Client.Extensions
{
    public class RequireAuthenticationAttribute : Attribute, IPageFilter
    {
        private readonly string[] _roles;

        public RequireAuthenticationAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var accessToken = context.HttpContext.Session.GetString("AccessToken");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (string.IsNullOrEmpty(accessToken))
            {
                context.Result = new RedirectToPageResult("/Login");
                return;
            }

            // Kiểm tra quyền nếu có yêu cầu vai trò cụ thể
            if (_roles != null && _roles.Length > 0)
            {
                if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
                {
                    context.Result = new RedirectToPageResult("/AccessDenied");
                    return;
                }
            }
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }
    }
} 