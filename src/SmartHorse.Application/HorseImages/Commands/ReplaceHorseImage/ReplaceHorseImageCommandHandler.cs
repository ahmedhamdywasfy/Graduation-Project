using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.HorseImages.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Commands.ReplaceHorseImage;

/// <summary>
/// Replaces an existing gallery image's file while preserving its position and
/// main/primary flag (Sprint 2 §5 "Replace Image") — implemented as an upload
/// of the new file followed by removal of the old one, rather than a single
/// remote "overwrite" call, since Cloudinary's overwrite mode would require a
/// stable public_id scheme this design doesn't use (UniqueFilename=true, so
/// every upload gets its own id — see CloudinaryImageStorageService).
/// </summary>
public class ReplaceHorseImageCommandHandler : IRequestHandler<ReplaceHorseImageCommand, HorseGalleryImageDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public ReplaceHorseImageCommandHandler(
        IHorseRepository horseRepository,
        IImageStorageService imageStorageService,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _horseRepository = horseRepository;
        _imageStorageService = imageStorageService;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<HorseGalleryImageDto> Handle(ReplaceHorseImageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        var existingImage = horse.Images.FirstOrDefault(i => i.Id == request.ImageId)
            ?? throw new NotFoundException(nameof(HorseImage), request.ImageId);

        var oldStorageId = existingImage.StorageId;
        var wasPrimary = existingImage.IsPrimary;
        var displayOrder = existingImage.DisplayOrder;

        var contentHash = await ComputeContentHashAsync(request.Content, cancellationToken);
        if (horse.Images.Any(i => i.Id != request.ImageId && i.ContentHash == contentHash))
        {
            throw new DuplicateHorseImageException();
        }

        var uploadResult = await _imageStorageService.UploadAsync(
            request.HorseId, request.Content, request.FileName, request.ContentType, cancellationToken);

        horse.RemoveImage(request.ImageId);
        var newImage = horse.AddImage(
            uploadResult.Url, uploadResult.StorageId, uploadResult.ContentType, uploadResult.FileSizeBytes,
            uploadResult.Width, uploadResult.Height, contentHash, wasPrimary);
        newImage.UpdateDisplayOrder(displayOrder);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _imageStorageService.DeleteAsync(oldStorageId, cancellationToken);

        return _mapper.Map<HorseGalleryImageDto>(newImage);
    }

    private static async Task<string> ComputeContentHashAsync(Stream content, CancellationToken cancellationToken)
    {
        content.Position = 0;
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content, cancellationToken);
        content.Position = 0;
        return Convert.ToHexString(hashBytes);
    }
}
