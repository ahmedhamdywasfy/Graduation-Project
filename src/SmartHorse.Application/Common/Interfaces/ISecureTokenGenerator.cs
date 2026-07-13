namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Generic cryptographically-secure random token generation + hashing, shared by
/// the refresh-token flow (IJwtService) use case and the password-reset flow.
/// Kept separate from IJwtService because it is not JWT-specific — password reset
/// tokens are opaque, single-use, short-lived values, not JWTs.
/// </summary>
public interface ISecureTokenGenerator
{
    string GenerateToken();

    string HashToken(string token);
}
