namespace SmartHorse.Application.Common.Models;

/// <summary>Result of a successful upload through <c>IImageStorageService</c> (Sprint 2 §7).</summary>
public record ImageUploadResult(
    string Url,
    string StorageId,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height);
