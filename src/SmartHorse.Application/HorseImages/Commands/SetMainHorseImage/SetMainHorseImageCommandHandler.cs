using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Commands.SetMainHorseImage;

public class SetMainHorseImageCommandHandler : IRequestHandler<SetMainHorseImageCommand>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IApplicationDbContext _dbContext;

    public SetMainHorseImageCommandHandler(IHorseRepository horseRepository, IApplicationDbContext dbContext)
    {
        _horseRepository = horseRepository;
        _dbContext = dbContext;
    }

    public async Task Handle(SetMainHorseImageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        horse.SetMainImage(request.ImageId);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
