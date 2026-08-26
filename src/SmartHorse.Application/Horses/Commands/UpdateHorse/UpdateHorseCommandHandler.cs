using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Horses.Commands.UpdateHorse;

public class UpdateHorseCommandHandler : IRequestHandler<UpdateHorseCommand, HorseDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IBreedRepository _breedRepository;
    private readonly IColorRepository _colorRepository;
    private readonly IGenderRepository _genderRepository;
    private readonly IHorseStatusRepository _horseStatusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateHorseCommandHandler(
        IHorseRepository horseRepository,
        IBreedRepository breedRepository,
        IColorRepository colorRepository,
        IGenderRepository genderRepository,
        IHorseStatusRepository horseStatusRepository,
        ICurrentUserService currentUser,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _horseRepository = horseRepository;
        _breedRepository = breedRepository;
        _colorRepository = colorRepository;
        _genderRepository = genderRepository;
        _horseStatusRepository = horseStatusRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<HorseDto> Handle(UpdateHorseCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.Id);

        if (!await _breedRepository.ExistsAsync(request.BreedId, cancellationToken))
        {
            throw new NotFoundException(nameof(Breed), request.BreedId);
        }

        if (!await _colorRepository.ExistsAsync(request.ColorId, cancellationToken))
        {
            throw new NotFoundException(nameof(Color), request.ColorId);
        }

        if (!await _genderRepository.ExistsAsync(request.GenderId, cancellationToken))
        {
            throw new NotFoundException(nameof(Gender), request.GenderId);
        }

        if (!await _horseStatusRepository.ExistsAsync(request.StatusId, cancellationToken))
        {
            throw new NotFoundException(nameof(HorseStatus), request.StatusId);
        }

        if (!string.IsNullOrWhiteSpace(request.MicrochipNumber)
            && await _horseRepository.MicrochipNumberExistsAsync(request.MicrochipNumber, request.Id, cancellationToken))
        {
            throw new DuplicateMicrochipNumberException(request.MicrochipNumber);
        }

        if (!string.IsNullOrWhiteSpace(request.RegistrationNumber)
            && await _horseRepository.RegistrationNumberExistsAsync(request.RegistrationNumber, request.Id, cancellationToken))
        {
            throw new DuplicateRegistrationNumberException(request.RegistrationNumber);
        }

        horse.UpdateDetails(
            request.Name,
            request.BreedId,
            request.ColorId,
            request.GenderId,
            request.StatusId,
            request.Weight,
            request.Height,
            request.BirthDate,
            request.Description,
            request.MicrochipNumber,
            request.RegistrationNumber,
            updatedBy: _currentUser.UserId ?? Guid.Empty);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _horseRepository.GetByIdAsync(horse.Id, cancellationToken);
        return _mapper.Map<HorseDto>(updated);
    }
}
