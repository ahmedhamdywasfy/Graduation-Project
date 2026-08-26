using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Commands.ReorderHorseImages;

public class ReorderHorseImagesCommandHandler : IRequestHandler<ReorderHorseImagesCommand>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IApplicationDbContext _dbContext;

    public ReorderHorseImagesCommandHandler(IHorseRepository horseRepository, IApplicationDbContext dbContext)
    {
        _horseRepository = horseRepository;
        _dbContext = dbContext;
    }

    public async Task Handle(ReorderHorseImagesCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        horse.ReorderImages(request.OrderedImageIds);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
