using Api.Application.Common.Interfaces;
using Api.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Infrastructure.Services;

/// <summary>
/// Generates a signed JWT containing user identity and permission claims.
/// Configuration is read from appsettings.json under "Jwt" section.
/// </summary>
public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public string GenerateToken(User user, Guid sessionId)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = jwtSettings["Issuer"] ?? "api";
        var audience = jwtSettings["Audience"] ?? "bff";
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "15");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name,  user.FullName),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("sid", sessionId.ToString()),
        };

        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Name)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
