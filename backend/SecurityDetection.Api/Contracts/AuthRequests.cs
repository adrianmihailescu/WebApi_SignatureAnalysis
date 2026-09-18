public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int UserId, string Username, string Role);
