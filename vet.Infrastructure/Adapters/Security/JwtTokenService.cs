
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using vet.Application.Services;
using vet.Domain.Entities;

namespace vet.Infrastructure.Adapters.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, string roleName)
    {
        var secret = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "La clave JWT no está configurada.");

        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException(
                "La clave JWT debe tener al menos 32 bytes.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer no configurado.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience no configurado.");

        var claims = new[]
        {
            new Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(ClaimTypes.Role, roleName),

            new Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
