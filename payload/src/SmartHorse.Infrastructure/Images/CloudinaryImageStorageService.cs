using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Infrastructure.Images;

/// <summary>
/// Cloudinary-backed implementation of <see cref="IImageStorageService"/>
/// (Sprint 2 §7). Validates extension, content type, file size, and pixel
/// dimensions locally — via SixLabors.ImageSharp's header-only
/// <see cref="Image.Identify(Stream)"/>, which reads just enough of the file to
/// determine dimensions without decoding the full image — before ever calling
/// Cloudinary, so invalid uploads never cost an API call or storage quota.
/// Nothing outside this class or <see cref="CloudinarySettings"/> references
/// CloudinaryDotNet — swapping to Azure Blob Storage means writing one new
/// class implementing <see cref="IImageStorageService"/> and changing one DI
/// registration in <c>DependencyInjection.cs</c>.
/// </summary>
public class CloudinaryImageStorageService : IImageStorageService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _cloudinarySettings;
    private readonly ImageValidationSettings _validationSettings;

    public CloudinaryImageStorageService(IOptions<CloudinarySettings> cloudinarySettings, IOptions<ImageValidationSettings> validationSettings)
    {
        _cloudinarySettings = cloudinarySettings.Value;
        _validationSettings = validationSettings.Value;

        var account = new Account(_cloudinarySettings.CloudName, _cloudinarySettings.ApiKey, _cloudinarySettings.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<SmartHorse.Application.Common.Models.ImageUploadResult> UploadAsync(
        Guid horseId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.ContainsKey(contentType))
        {
            throw new UnsupportedFileTypeException(contentType);
        }

        content.Position = 0;
        var fileSizeBytes = content.Length;

        if (fileSizeBytes < _validationSettings.MinFileSizeBytes)
        {
            throw new FileTooSmallException(_validationSettings.MinFileSizeBytes);
        }

        if (fileSizeBytes > _validationSettings.MaxFileSizeBytes)
        {
            throw new FileTooLargeException(_validationSettings.MaxFileSizeBytes);
        }

        content.Position = 0;
        var (width, height) = await IdentifyDimensionsAsync(content, cancellationToken);

        if (width < _validationSettings.MinWidthPixels || height < _validationSettings.MinHeightPixels)
        {
            throw InvalidImageDimensionsException.TooSmall(_validationSettings.MinWidthPixels, _validationSettings.MinHeightPixels);
        }

        if (width > _validationSettings.MaxWidthPixels || height > _validationSettings.MaxHeightPixels)
        {
            throw InvalidImageDimensionsException.TooLarge(_validationSettings.MaxWidthPixels, _validationSettings.MaxHeightPixels);
        }

        content.Position = 0;
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = $"{_cloudinarySettings.RootFolder}/{horseId}",
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return new SmartHorse.Application.Common.Models.ImageUploadResult(
            result.SecureUrl.ToString(),
            result.PublicId,
            contentType,
            fileSizeBytes,
            result.Width,
            result.Height);
    }

    public async Task DeleteAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var deleteParams = new DeletionParams(storageId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    private static async Task<(int Width, int Height)> IdentifyDimensionsAsync(Stream content, CancellationToken cancellationToken)
    {
        try
        {
            var info = await Image.IdentifyAsync(content, cancellationToken);
            if (info is null)
            {
                throw new UnsupportedFileTypeException("unknown");
            }

            return (info.Width, info.Height);
        }
        catch (UnknownImageFormatException)
        {
            throw new UnsupportedFileTypeException("unknown");
        }
        finally
        {
            content.Position = 0;
        }
    }
}
