using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthExample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ValuesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "You accessed a protected endpoint.",
            user = User.Identity?.Name,
            roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmin()
    {
        return Ok(new
        {
            message = "Admin-only endpoint.",
            user = User.Identity?.Name
        });
    }
}
