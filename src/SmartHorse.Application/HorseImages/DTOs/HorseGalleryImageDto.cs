namespace SmartHorse.Application.HorseImages.DTOs;

/// <summary>A single gallery image with full Sprint 2 §5 metadata (distinct from Sprint 1's minimal Horses-module HorseImageDto).</summary>
public class HorseGalleryImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
