using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CollegeEventManagement.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CollegeEventManagement.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public Task<(string Token, DateTime Expiration)> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "CollegeEventManagement_Super_Secret_Key_2026_Secure_Token_Key_9876543210!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var durationInMinutes = int.TryParse(jwtSettings["DurationInMinutes"], out var minutes) ? minutes : 1440;
        var expiration = DateTime.UtcNow.AddMinutes(durationInMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.StudentId))
        {
            claims.Add(new Claim("StudentId", user.StudentId));
        }

        if (!string.IsNullOrEmpty(user.Department))
        {
            claims.Add(new Claim("Department", user.Department));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = jwtSettings["Issuer"] ?? "CollegeEventManagementAPI",
            Audience = jwtSettings["Audience"] ?? "CollegeEventManagementClients",
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Task.FromResult((tokenHandler.WriteToken(token), expiration));
    }
}
