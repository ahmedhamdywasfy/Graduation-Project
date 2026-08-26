using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartHorse.Domain.Entities;
using SmartHorse.Infrastructure.Identity;

namespace SmartHorse.API.Extensions;

/// <summary>
/// JWT bearer authentication configuration (v0.1 Section 19 / v0.2 Section 8).
/// Verifies tokens using only the RSA public key — the API validates its own
/// tokens here, but the same public key can be shared with other future services
/// (e.g., the AI microservice) so they can verify tokens without holding the
/// private signing key.
///
/// Sprint 2 change: <see cref="JwtBearerOptions"/> is configured lazily via
/// <c>IOptions&lt;JwtSettings&gt;</c> (through <c>AddOptions(...).Configure&lt;&gt;</c>)
/// instead of reading configuration eagerly at service-registration time. This
/// makes JWT setup immune to configuration-source ordering (important for
/// SmartHorse.API.IntegrationTests' <c>WebApplicationFactory</c>-injected test
/// keys, Sprint 2 §14) and ensures <see cref="JwtSettingsValidator"/>'s
/// ValidateOnStart check runs against the exact same settings object actually
/// used to build the signing key.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddSmartHorseAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((jwtBearerOptions, jwtSettingsOptions) =>
            {
                var settings = jwtSettingsOptions.Value;
                using var rsa = RSA.Create();

                    var publicKey = settings.PublicKeyPem.Replace("\\n", Environment.NewLine);

                    rsa.ImportFromPem(publicKey);

                    // Shared KeyId with the signing side (JwtSigningKeyProvider) so
                    // tokens carry an explicit "kid" and validation matches on key
                    // identity rather than implicit key-material probing. Also gives
                    // Microsoft.IdentityModel.Tokens' internal signature-provider
                    // cache a deterministic identity for this key — see the XML doc
                    // on IJwtSigningKeyProvider for the full root-cause explanation
                    // of why that cache matters here.
                    var signingKey = new RsaSecurityKey(RSA.Create(rsa.ExportParameters(false)))
                    {
                        KeyId = JwtSigningKeyProvider.KeyId
                    };

                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            // Administrator-only policy, used by the Admin-facing endpoints
            // introduced in Sprint 1 (GetUsers, DeactivateUser) and extended by
            // the full Admin module in later sprints (v0.2 Section 2).
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole(Role.Names.Administrator));

            // Person 2 Sprint 1 §12 — write access to Horse Core (create/update/
            // delete/restore) is limited to Administrator, Owner, and Veterinarian;
            // every other authenticated role gets read-only access via the plain
            // [Authorize] attribute on the read endpoints in HorsesController.
            options.AddPolicy("CanManageHorses", policy =>
                policy.RequireRole(Role.Names.Administrator, Role.Names.Owner, Role.Names.Veterinarian));
        });

        return services;
    }
}
