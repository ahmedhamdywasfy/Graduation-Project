using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A horse lifecycle status (Active, ForSale, Sold, Retired — matching the
/// values already documented for Horses.Status in the approved v0.1 §13 schema).
/// Person 2 Sprint 1 Database Design §1 requires this as its own reference table
/// rather than a free-text/enum column, consistent with Breed/Color/Gender.
/// </summary>
public class HorseStatus : BaseIntEntity
{
    private HorseStatus()
    {
        Name = string.Empty;
    }

    public HorseStatus(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Status name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; private set; }

    public static class Names
    {
        public const string Active = "Active";
        public const string ForSale = "ForSale";
        public const string Sold = "Sold";
        public const string Retired = "Retired";

        public static readonly IReadOnlyList<string> All = new[] { Active, ForSale, Sold, Retired };
    }
}
