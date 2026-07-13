using System.Security.Cryptography;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SmartHorse.Infrastructure.Identity;

namespace SmartHorse.Infrastructure.HealthChecks;

/// <summary>
/// Verifies the JWT signing keys are present and parse as valid RSA PEM material
/// (Sprint 2 §11 — Health Checks: Authentication). This catches a broken/missing
/// key deployment before it surfaces as every login failing.
/// </summary>
public class AuthenticationHealthCheck : IHealthCheck
{
    private readonly JwtSettings _settings;

    public AuthenticationHealthCheck(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.PrivateKeyPem) || string.IsNullOrWhiteSpace(_settings.PublicKeyPem))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("JWT signing keys are not configured."));
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_settings.PrivateKeyPem.Replace("\\n", Environment.NewLine));
            using var rsaPublic = RSA.Create();
            rsaPublic.ImportFromPem(_settings.PublicKeyPem.Replace("\\n", Environment.NewLine));

            return Task.FromResult(HealthCheckResult.Healthy("JWT signing keys are valid."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("JWT signing keys are invalid.", ex));
        }
    }
}
