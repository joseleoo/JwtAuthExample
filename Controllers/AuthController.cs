using JwtAuthExample.Models;
using JwtAuthExample.Services;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly List<AuthUser> Users =
    [
        new(1, "admin", "Admin123!", ["Admin"]),
        new(2, "user", "User123!", ["User"])
    ];

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
        return Ok(new { token });
    }

    private record AuthUser(int Id, string Username, string Password, string[] Roles);
}
