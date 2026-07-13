namespace SmartHorse.Infrastructure.Services;

/// <summary>
/// Bound from the "FileStorage" configuration section. This Sprint 2 implementation
/// writes to local disk under the API's wwwroot; v0.2 §6 targets Azure Blob Storage
/// for the eventual multi-instance deployment — see <see cref="IFileStorageService"/>
/// for why that swap won't require caller changes.
/// </summary>
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute or relative-to-content-root path where avatars are written.</summary>
    public string AvatarStoragePath { get; set; } = "wwwroot/avatars";

    /// <summary>Public base URL avatars are served from (paired with UseStaticFiles in the API layer).</summary>
    public string PublicBaseUrl { get; set; } = "/avatars";

    public long MaxAvatarSizeBytes { get; set; } = 2 * 1024 * 1024; // 2 MB
}
