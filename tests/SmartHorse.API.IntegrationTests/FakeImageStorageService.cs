using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;

namespace SmartHorse.API.IntegrationTests;

/// <summary>
/// In-memory stand-in for <see cref="IImageStorageService"/> used only by
/// integration tests (Sprint 2 §16) — hitting real Cloudinary from automated
/// tests isn't feasible or desirable. Registered in place of
/// <c>CloudinaryImageStorageService</c> by <see cref="CustomWebApplicationFactory"/>,
/// the same way the InMemory EF Core provider replaces SQL Server for these
/// tests. Still performs the same size/dimension-adjacent bookkeeping a real
/// implementation would return, so handler logic that reads the result is
/// exercised realistically.
/// </summary>
public class FakeImageStorageService : IImageStorageService
{
    public Task<ImageUploadResult> UploadAsync(Guid horseId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        content.Position = 0;
        var sizeBytes = content.Length;
        var storageId = $"fake/{horseId}/{Guid.NewGuid():N}";
        var url = $"https://fake-cdn.test/{storageId}.jpg";

        return Task.FromResult(new ImageUploadResult(url, storageId, contentType, sizeBytes, 800, 600));
    }

    public Task DeleteAsync(string storageId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
