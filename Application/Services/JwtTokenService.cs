using Domain.Models;
using System.Text;
using Application.Interfaces;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Application.Services;

public class JwtTokenService : ITokenService
{
    public string GetAccessToken(Account account)
    {
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Name)
        };

        // Алгоритм кодирования токена
        var secretKey = "kiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoy";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            signingCredentials: signingCredentials,
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: claims
            );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return tokenString;
    }
    public string GetRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}
