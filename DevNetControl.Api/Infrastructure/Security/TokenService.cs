using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevNetControl.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace DevNetControl.Api.Infrastructure.Security;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("UserId", user.Id.ToString()),
            new Claim("TenantId", user.TenantId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(15), // Access token corto
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return (accessToken, refreshToken);
    }
}
