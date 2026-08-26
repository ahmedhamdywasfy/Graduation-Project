using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.HorseImages.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Commands.UploadHorseImage;

/// <summary>
/// Uploads an image to remote storage (Sprint 2 §7 — IImageStorageService/
/// Cloudinary) and attaches it to the horse's gallery. Content-type/size/
/// dimension validation happens inside <see cref="IImageStorageService"/> so
/// every caller gets the same enforcement (Sprint 2 §6); duplicate-image and
/// max-image-count checks happen in <see cref="Horse.AddImage"/> since they
/// depend on the horse's existing gallery, which only the aggregate can see.
/// </summary>
public class UploadHorseImageCommandHandler : IRequestHandler<UploadHorseImageCommand, HorseGalleryImageDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UploadHorseImageCommandHandler(
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

    public async Task<HorseGalleryImageDto> Handle(UploadHorseImageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        if (horse.Images.Count >= Horse.MaxImageCount)
        {
            throw new MaxImagesExceededException(Horse.MaxImageCount);
        }

        var contentHash = await ComputeContentHashAsync(request.Content, cancellationToken);
        if (horse.Images.Any(i => i.ContentHash == contentHash))
        {
            throw new DuplicateHorseImageException();
        }

        var uploadResult = await _imageStorageService.UploadAsync(
            request.HorseId, request.Content, request.FileName, request.ContentType, cancellationToken);

        var image = horse.AddImage(
            uploadResult.Url,
            uploadResult.StorageId,
            uploadResult.ContentType,
            uploadResult.FileSizeBytes,
            uploadResult.Width,
            uploadResult.Height,
            contentHash,
            request.IsPrimary);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<HorseGalleryImageDto>(image);
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
