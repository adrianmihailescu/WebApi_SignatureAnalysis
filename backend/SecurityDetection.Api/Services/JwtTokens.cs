using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtTokens
{
    public static string Create(User user, IConfiguration configuration)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(configuration.GetValue("Jwt:ExpirationHours", 8)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
