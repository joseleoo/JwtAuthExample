namespace JwtAuthExample.Models;

public static class JwtSettings
{
    // SIMULATED SECRET STORE: hardcoded signing key. In production this would
    // come from a secret manager or environment variable, never source code.
    public const string Key = "SuperSecretKeyForJwtTokenAuthorization123456789";
    public const string Issuer = "JwtAuthExample";
    public const string Audience = "JwtAuthExampleUsers";
    public const int DurationInMinutes = 60;
    public const int RefreshTokenDurationInDays = 7;
}
