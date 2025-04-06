using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MedicalWareHouse_Client.Extensions
{
    public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;

        public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
            _options = options.Value;
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Tạo chính sách yêu cầu xác thực cơ bản
            if (policyName == "RequireAuthentication")
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // Tạo chính sách cho các vai trò cụ thể
            if (policyName.StartsWith("RequireRole:"))
            {
                var role = policyName.Substring("RequireRole:".Length);
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(role)
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return base.GetPolicyAsync(policyName);
        }
    }
} 