using System.ComponentModel.DataAnnotations;

namespace JwtAuthExample.Models;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);
