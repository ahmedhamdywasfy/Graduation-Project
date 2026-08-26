using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>A horse coat color reference value (e.g. Bay, Chestnut) — Person 2 Sprint 1 Database Design §1.</summary>
public class Color : BaseIntEntity
{
    private Color()
    {
        Name = string.Empty;
    }

    public Color(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Color name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; private set; }
}
