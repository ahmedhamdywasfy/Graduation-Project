using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// BCrypt-based implementation of <see cref="IPasswordHasher"/> (v0.1 Section 21 /
/// v0.2 Section 8). BCrypt.Net-Next automatically embeds a random salt per hash and
/// handles the work-factor, so no separate salt storage/management is needed.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Defensive: a corrupted/foreign hash format should fail closed, not throw past the caller.
            return false;
        }
    }
}
