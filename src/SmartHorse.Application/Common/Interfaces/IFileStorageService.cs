namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Abstraction over binary file storage, used in Sprint 2 for avatar uploads.
/// The v0.2 §6 target architecture is Azure Blob Storage with SAS-token direct
/// upload; this Sprint 2 Infrastructure implementation is a local-disk provider
/// behind the same interface so the Horse/Medical-attachment sprints and a future
/// Azure Blob implementation can both slot in without touching callers.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file and returns a publicly reachable URL/path for it. Throws
    /// <see cref="SmartHorse.Domain.Exceptions.UnsupportedFileTypeException"/> or
    /// <see cref="SmartHorse.Domain.Exceptions.FileTooLargeException"/> if validation fails.
    /// </summary>
    Task<string> SaveAvatarAsync(Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
