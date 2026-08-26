using SmartHorse.Application.Common.Models;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Abstraction over remote image storage for the Horse Images module (Sprint 2
/// §7). Deliberately separate from Person 1 Sprint 2's <c>IFileStorageService</c>
/// (which is local-disk, avatar-specific) — horse gallery images are a distinct
/// concern with distinct metadata needs (dimensions, content hash, a
/// deletable remote asset id). The Sprint 2 Infrastructure implementation is
/// Cloudinary-backed (<c>CloudinaryImageStorageService</c>); nothing in the
/// Application layer references CloudinaryDotNet directly, so swapping to
/// Azure Blob Storage later means writing one new Infrastructure class and
/// changing one DI registration — no caller changes.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Uploads an image and returns its URL plus derived metadata (dimensions,
    /// size, a provider-specific storage id for later deletion). Content-type,
    /// size, and dimension validation happen inside the implementation so every
    /// caller gets the same enforcement (Sprint 2 §6 — Image Validation).
    /// </summary>
    Task<ImageUploadResult> UploadAsync(Guid horseId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageId, CancellationToken cancellationToken = default);
}
