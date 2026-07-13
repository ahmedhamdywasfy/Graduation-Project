namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Bound from the "Jwt" configuration section (appsettings.json / environment
/// variables / Key Vault in production — never committed secrets, v0.1 Section 21).
/// RS256 asymmetric signing per v0.2 Security Review, Section 8: PrivateKeyPem
/// signs tokens (API only), PublicKeyPem verifies them (shareable with future
/// services, e.g. the AI microservice, without exposing the signing secret).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
