using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Commands.DeleteHorseImage;

/// <summary>
/// Removes an image from both the gallery and remote storage (Sprint 2 §5, §8).
/// The remote delete happens after the database change is saved — if the
/// remote call fails, the horse's gallery is already correct and consistent;
/// an orphaned remote asset is a lower-severity problem than a broken gallery.
/// </summary>
public class DeleteHorseImageCommandHandler : IRequestHandler<DeleteHorseImageCommand>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly IApplicationDbContext _dbContext;

    public DeleteHorseImageCommandHandler(
        IHorseRepository horseRepository,
        IImageStorageService imageStorageService,
        IApplicationDbContext dbContext)
    {
        _horseRepository = horseRepository;
        _imageStorageService = imageStorageService;
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteHorseImageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        var image = horse.Images.FirstOrDefault(i => i.Id == request.ImageId)
            ?? throw new NotFoundException(nameof(HorseImage), request.ImageId);

        var storageId = image.StorageId;

        horse.RemoveImage(request.ImageId);
        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _imageStorageService.DeleteAsync(storageId, cancellationToken);
    }
}
