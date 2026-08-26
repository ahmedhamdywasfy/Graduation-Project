using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>A horse gender/sex classification (e.g. Stallion, Mare, Gelding) — Person 2 Sprint 1 Database Design §1.</summary>
public class Gender : BaseIntEntity
{
    private Gender()
    {
        Name = string.Empty;
    }

    public Gender(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Gender name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; private set; }
}
