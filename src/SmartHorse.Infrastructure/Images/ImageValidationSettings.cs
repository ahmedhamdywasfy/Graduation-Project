namespace SmartHorse.Infrastructure.Images;

/// <summary>Bound from the "ImageValidation" configuration section — Sprint 2 §6 Image Validation thresholds.</summary>
public class ImageValidationSettings
{
    public const string SectionName = "ImageValidation";

    public long MinFileSizeBytes { get; set; } = 1024;               // 1 KB — rejects empty/corrupt uploads
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;     // 5 MB
    public int MinWidthPixels { get; set; } = 200;
    public int MinHeightPixels { get; set; } = 200;
    public int MaxWidthPixels { get; set; } = 8000;
    public int MaxHeightPixels { get; set; } = 8000;
}
