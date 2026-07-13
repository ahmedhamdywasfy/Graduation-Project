using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Issues and validates JWT access tokens (v0.2 Security Review, Section 8 — RS256
/// asymmetric signing so other services, e.g. the future AI microservice, can verify
/// tokens with only the public key).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a short-lived (10–15 min) signed access token containing the
    /// user's Id and role claims.
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    /// <summary>Generates a cryptographically random opaque refresh token value.</summary>
    string GenerateRefreshTokenValue();

    /// <summary>Hashes a refresh token value for storage (raw value is never persisted).</summary>
    string HashRefreshToken(string refreshTokenValue);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }
}
