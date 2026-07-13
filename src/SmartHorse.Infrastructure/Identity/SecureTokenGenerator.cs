using System.Security.Cryptography;
using System.Text;
using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Cryptographically secure random token generation and SHA-256 hashing, shared by
/// the refresh-token flow and the password-reset flow. Raw token values are only
/// ever held in memory long enough to send to the client/email; only the hash is
/// persisted (v0.2 Security Review, Section 8).
/// </summary>
public class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenBytesLength = 32;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytesLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
