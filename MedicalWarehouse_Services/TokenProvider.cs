using MedicalWarehouse_BusinessObject.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;

namespace MedicalWarehouse_Services
{
    public class TokenProvider
    {
        private readonly IConfiguration _config;
        private readonly IConfigurationSection _jwtSettings;
        private readonly UserManager<User> _userManager;

        public TokenProvider(IConfiguration configuration, UserManager<User> userManager)
        {
            _config = configuration;
            _jwtSettings = _config.GetSection("JWT");
            _userManager = userManager;
        }

        public async Task<string> Create(User user)
        {
            var authSecret = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings["Secret"])
            );
            var userRole = await _userManager.GetRolesAsync(user);
            var tokenHandle = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("uid", user.Id),
                    new Claim("name", user.UserName),
                    new Claim("email", user.Email),
                    new Claim(ClaimTypes.Role, string.Join(",", userRole))
                }),
                Expires = DateTime.UtcNow.AddMinutes(60),
                Audience = _jwtSettings["Audience"],
                Issuer = _jwtSettings["Issuer"],
                SigningCredentials = new SigningCredentials(
                    authSecret,
                    SecurityAlgorithms.HmacSha256
                )
            };

            var token = tokenHandle.CreateToken(tokenDescriptor);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string Read(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwtSecurityToken = handler.ReadJwtToken(jwt);

            var emailClaim = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email);

            return emailClaim?.Value;
        }


        public string CreateRandomToken()
        {
            var authSecret = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings["Secret"])
            );
            var tokenHandle = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(60*3),
                Audience = _jwtSettings["Audience"],
                Issuer = _jwtSettings["Issuer"],
                SigningCredentials = new SigningCredentials(
                    authSecret,
                    SecurityAlgorithms.HmacSha256
                )
            };

            var token = tokenHandle.CreateToken(tokenDescriptor);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
