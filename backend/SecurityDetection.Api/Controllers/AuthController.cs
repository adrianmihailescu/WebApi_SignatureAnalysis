using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == request.Username, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(new LoginResponse(JwtTokens.Create(user, configuration), user.Id, user.Username, user.Role));
    }
}
