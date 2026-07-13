using Microsoft.Extensions.Options;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Infrastructure.Services;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorageService"/> (Sprint 2 §3,
/// §9 — Secure File Upload). Validates content type and size before touching the
/// filesystem, writes under a per-user folder to avoid filename collisions, and
/// returns a URL servable by the API's static file middleware.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly FileStorageSettings _settings;

    public LocalFileStorageService(IOptions<FileStorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> SaveAvatarAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            throw new UnsupportedFileTypeException(contentType);
        }

        if (content.Length > _settings.MaxAvatarSizeBytes)
        {
            throw new FileTooLargeException(_settings.MaxAvatarSizeBytes);
        }

        var directory = Path.Combine(_settings.AvatarStoragePath, userId.ToString());
        Directory.CreateDirectory(directory);

        // Fixed filename per user (not the caller-supplied name) — avoids path
        // traversal entirely and means re-uploading simply overwrites the old avatar.
        var storedFileName = $"avatar{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            content.Position = 0;
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{userId}/{storedFileName}?v={DateTime.UtcNow.Ticks}";
    }
}
