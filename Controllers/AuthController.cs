using System.Collections.Concurrent;
using System.Security.Cryptography;
using JwtAuthExample.Models;
using JwtAuthExample.Services;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthExample.Controllers;

// This controller is the sample server's auth API. Only the database layer is
// simulated (see SIMULATED DATABASE comments). There are no external API or
// LLM calls in this project.
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    // SIMULATED DATABASE: hardcoded users table. In production this would be
    // queried from a real database (e.g., SELECT * FROM Users WHERE ...).
    private static readonly List<AuthUser> Users =
    [
        new(1, "admin", "Admin123!", ["Admin"]),
        new(2, "user", "User123!", ["User"])
    ];

    // SIMULATED DATABASE: in-memory refresh-token table. In production this
    // would be a database table or a distributed cache (e.g., Redis).
    private static readonly ConcurrentDictionary<string, RefreshToken> RefreshTokens = new();

    private static readonly CookieOptions RefreshTokenCookie = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        MaxAge = TimeSpan.FromDays(JwtSettings.RefreshTokenDurationInDays)
    };

    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = Users.FirstOrDefault(
            u => u.Username == request.Username && u.Password == request.Password);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = _tokenService.GenerateToken(user.Id, user.Username, user.Roles);
        var refreshToken = CreateRefreshToken(user);

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, RefreshTokenCookie);

        return Ok(new { token });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken) ||
            !RefreshTokens.TryGetValue(refreshToken, out var stored))
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        if (stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            RefreshTokens.TryRemove(refreshToken, out _);
            Response.Cookies.Delete(RefreshTokenCookieName, RefreshTokenCookie);
            return Unauthorized(new { message = "Refresh token has expired." });
        }

        var user = Users.FirstOrDefault(u => u.Id == stored.UserId);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        // Rotate the refresh token: the used one is invalidated.
        RefreshTokens.TryRemove(refreshToken, out _);

        var token = _tokenService.GenerateToken(user.Id, user.Username, user.Roles);
        var newRefreshToken = CreateRefreshToken(user);

        Response.Cookies.Append(RefreshTokenCookieName, newRefreshToken, RefreshTokenCookie);

        return Ok(new { token });
    }

    private static string CreateRefreshToken(AuthUser user)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        RefreshTokens[refreshToken] = new RefreshToken(
            user.Id,
            DateTime.UtcNow.AddDays(JwtSettings.RefreshTokenDurationInDays));

        return refreshToken;
    }

    // SIMULATED DB ROW: users table row.
    private record AuthUser(int Id, string Username, string Password, string[] Roles);

    // SIMULATED DB ROW: refresh-tokens table row.
    private record RefreshToken(int UserId, DateTime ExpiresAtUtc);
}
