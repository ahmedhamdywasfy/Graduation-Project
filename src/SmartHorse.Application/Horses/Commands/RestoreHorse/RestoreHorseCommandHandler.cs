using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Horses.Commands.RestoreHorse;

/// <summary>
/// Restores a soft-deleted horse. Must look the horse up bypassing the
/// soft-delete query filter (<see cref="IHorseRepository.GetDeletedByIdAsync"/>),
/// since the normal <c>GetByIdAsync</c> would never find it.
/// </summary>
public class RestoreHorseCommandHandler : IRequestHandler<RestoreHorseCommand, HorseDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public RestoreHorseCommandHandler(IHorseRepository horseRepository, IApplicationDbContext dbContext, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<HorseDto> Handle(RestoreHorseCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetDeletedByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.Id);

        horse.RestoreFromDeletion();

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var restored = await _horseRepository.GetByIdAsync(horse.Id, cancellationToken);
        return _mapper.Map<HorseDto>(restored);
    }
}
