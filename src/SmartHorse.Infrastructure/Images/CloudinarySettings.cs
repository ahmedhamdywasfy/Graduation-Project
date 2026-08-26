namespace SmartHorse.Infrastructure.Images;

/// <summary>Bound from the "Cloudinary" configuration section. Credentials come from secrets/env, never hardcoded (Sprint 2 §7).</summary>
public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Folder prefix all horse images are uploaded under, e.g. "smarthorse/horses".</summary>
    public string RootFolder { get; set; } = "smarthorse/horses";
}
