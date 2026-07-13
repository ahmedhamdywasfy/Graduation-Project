namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Abstraction over password hashing (v0.1 Section 21 / v0.2 Section 8 — BCrypt).
/// Kept out of the Domain layer since it wraps a concrete cryptography library
/// (Infrastructure concern); Application handlers depend only on this interface.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);

    bool Verify(string plainTextPassword, string passwordHash);
}
