using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

public class JwtTokensTests
{
    [Fact]
    public void Create_EmitsExpectedIdentityAndRoleClaims()
    {
        var user = new User { Id = 7, Username = "analyst", Role = "SecurityAnalyst" };
        var token = JwtTokens.Create(user, Configuration());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("7", jwt.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("analyst", jwt.Claims.Single(x => x.Type == ClaimTypes.Name).Value);
        Assert.Equal("SecurityAnalyst", jwt.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void Create_EmitsConfiguredIssuerAudienceAndFutureExpiration()
    {
        var token = JwtTokens.Create(new User { Id = 1, Username = "admin", Role = "Admin" }, Configuration());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Equal("TestAudience", jwt.Audiences.Single());
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "Test-only-signing-key-with-at-least-32-characters",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpirationHours"] = "1"
        })
        .Build();
}
