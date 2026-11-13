namespace JwtAuthExample.Models;

public static class JwtSettings
{
    public const string Key = "SuperSecretKeyForJwtTokenAuthorization123456789";
    public const string Issuer = "JwtAuthExample";
    public const string Audience = "JwtAuthExampleUsers";
    public const int DurationInMinutes = 60;
}
