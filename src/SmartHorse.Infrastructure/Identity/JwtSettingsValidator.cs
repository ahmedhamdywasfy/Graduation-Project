using Microsoft.Extensions.Options;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Fails application startup fast if JWT signing configuration is missing or
/// malformed (Sprint 2 §9 — Configuration Validation), rather than discovering it
/// on the first login attempt in production. Registered via
/// <c>services.AddOptions&lt;JwtSettings&gt;().ValidateOnStart()</c>.
/// </summary>
public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PrivateKeyPem))
        {
            errors.Add("Jwt:PrivateKeyPem is required (RS256 private key, PEM format). See README for generation instructions.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicKeyPem))
        {
            errors.Add("Jwt:PublicKeyPem is required (RS256 public key, PEM format).");
        }

        if (options.AccessTokenLifetimeMinutes <= 0)
        {
            errors.Add("Jwt:AccessTokenLifetimeMinutes must be greater than zero.");
        }

        if (options.RefreshTokenLifetimeDays <= 0)
        {
            errors.Add("Jwt:RefreshTokenLifetimeDays must be greater than zero.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
