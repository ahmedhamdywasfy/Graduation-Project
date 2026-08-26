using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A photo attached to a <see cref="Horse"/> — table/relationship established in
/// Person 2 Sprint 1; the upload/gallery/ordering functionality and the
/// metadata fields below (<see cref="ContentType"/>, <see cref="FileSizeBytes"/>,
/// <see cref="Width"/>/<see cref="Height"/>, <see cref="ContentHash"/>,
/// <see cref="DisplayOrder"/>) are added in Sprint 2 §5–§6 (Horse Images /
/// Image Validation). Stored images live in Cloudinary (Sprint 2 §7);
/// <see cref="StorageId"/> is Cloudinary's public_id, needed to delete the
/// remote asset later without re-deriving it from the URL.
/// </summary>
public class HorseImage : BaseEntity
{
    private HorseImage()
    {
        ImageUrl = string.Empty;
        StorageId = string.Empty;
        ContentType = string.Empty;
        ContentHash = string.Empty;
    }

    public HorseImage(
        Guid horseId,
        string imageUrl,
        string storageId,
        string contentType,
        long fileSizeBytes,
        int width,
        int height,
        string contentHash,
        int displayOrder,
        bool isPrimary)
    {
        HorseId = horseId;
        ImageUrl = imageUrl;
        StorageId = storageId;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Width = width;
        Height = height;
        ContentHash = contentHash;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        UploadedAtUtc = DateTime.UtcNow;
    }

    public Guid HorseId { get; private set; }
    public Horse Horse { get; private set; } = null!;

    public string ImageUrl { get; private set; }

    /// <summary>Cloudinary public_id (or equivalent for a future storage provider) — needed to delete the remote asset.</summary>
    public string StorageId { get; private set; }

    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>SHA-256 of the file content, used to reject duplicate uploads for the same horse (Sprint 2 §6).</summary>
    public string ContentHash { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }

    public void SetAsMain()
    {
        IsPrimary = true;
    }

    public void UnsetMain()
    {
        IsPrimary = false;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}
