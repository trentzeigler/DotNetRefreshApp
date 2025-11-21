using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetRefreshApp.Models;
using Microsoft.IdentityModel.Tokens;

namespace DotNetRefreshApp.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            // TODO: Set JWT_SECRET in your .env file
            // Generate with: openssl rand -base64 32
            var secret = _configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
            
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("JWT_SECRET is not configured. Please set it in your .env file.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("emailVerified", user.EmailVerified.ToString())
            };

            var expirationDays = int.Parse(_configuration["JWT_EXPIRATION_DAYS"] ?? "7");
            
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expirationDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
