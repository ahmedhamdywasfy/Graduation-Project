using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A horse breed reference value (e.g. Arabian, Thoroughbred) — Person 2 Sprint 1
/// Database Design §1. Small, admin-managed lookup table, seeded at startup
/// (Infrastructure/Persistence/Seed/DbSeeder.cs), mirroring the existing
/// <see cref="Role"/> lookup-entity pattern from Person 1.
/// </summary>
public class Breed : BaseIntEntity
{
    private Breed()
    {
        Name = string.Empty;
    }

    public Breed(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Breed name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; private set; }
}
