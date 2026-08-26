using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A photo attached to a <see cref="Horse"/> — Person 2 Sprint 1 Database Design
/// §1. The table/relationship is established this sprint; a dedicated upload
/// endpoint (reusing the existing <c>IFileStorageService</c> abstraction from
/// Person 1 Sprint 2, the same way avatar upload does) is intentionally deferred
/// — see the Implementation Report's "Future Recommendations" — so this entity
/// has no rows written by any command yet other than through
/// <see cref="Horse.AddImage"/>, which nothing in this sprint calls from the API.
/// </summary>
public class HorseImage : BaseEntity
{
    private HorseImage()
    {
        ImageUrl = string.Empty;
    }

    public HorseImage(Guid horseId, string imageUrl, bool isPrimary)
    {
        HorseId = horseId;
        ImageUrl = imageUrl;
        IsPrimary = isPrimary;
        UploadedAtUtc = DateTime.UtcNow;
    }

    public Guid HorseId { get; private set; }
    public Horse Horse { get; private set; } = null!;

    public string ImageUrl { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
}
