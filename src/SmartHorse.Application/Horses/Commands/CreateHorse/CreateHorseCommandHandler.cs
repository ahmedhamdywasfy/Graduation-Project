using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Horses.Commands.CreateHorse;

/// <summary>
/// Creates a new horse. Resolves and validates all reference-data foreign keys
/// (Breed/Color/Gender/Status/Owner) before constructing the aggregate, checks
/// Microchip/Registration uniqueness, and records the initial
/// <see cref="OwnershipHistory"/> entry (Person 2 Sprint 1 §4, §9).
/// </summary>
public class CreateHorseCommandHandler : IRequestHandler<CreateHorseCommand, HorseDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IBreedRepository _breedRepository;
    private readonly IColorRepository _colorRepository;
    private readonly IGenderRepository _genderRepository;
    private readonly IHorseStatusRepository _horseStatusRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateHorseCommandHandler(
        IHorseRepository horseRepository,
        IBreedRepository breedRepository,
        IColorRepository colorRepository,
        IGenderRepository genderRepository,
        IHorseStatusRepository horseStatusRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _horseRepository = horseRepository;
        _breedRepository = breedRepository;
        _colorRepository = colorRepository;
        _genderRepository = genderRepository;
        _horseStatusRepository = horseStatusRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<HorseDto> Handle(CreateHorseCommand request, CancellationToken cancellationToken)
    {
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

        var statusId = request.StatusId;
        if (statusId.HasValue)
        {
            if (!await _horseStatusRepository.ExistsAsync(statusId.Value, cancellationToken))
            {
                throw new NotFoundException(nameof(HorseStatus), statusId.Value);
            }
        }
        else
        {
            var defaultStatus = await _horseStatusRepository.GetByNameAsync(HorseStatus.Names.Active, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Default horse status \"{HorseStatus.Names.Active}\" is not seeded. Run database seeding before creating horses.");
            statusId = defaultStatus.Id;
        }

        if (await _userRepository.GetByIdAsync(request.OwnerId, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(User), request.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(request.MicrochipNumber)
            && await _horseRepository.MicrochipNumberExistsAsync(request.MicrochipNumber, null, cancellationToken))
        {
            throw new DuplicateMicrochipNumberException(request.MicrochipNumber);
        }

        if (!string.IsNullOrWhiteSpace(request.RegistrationNumber)
            && await _horseRepository.RegistrationNumberExistsAsync(request.RegistrationNumber, null, cancellationToken))
        {
            throw new DuplicateRegistrationNumberException(request.RegistrationNumber);
        }

        var actingUserId = _currentUser.UserId;

        var horse = new Horse(
            request.Name,
            request.BreedId,
            request.ColorId,
            request.GenderId,
            statusId.Value,
            request.Weight,
            request.Height,
            request.BirthDate,
            request.OwnerId,
            request.Description,
            request.MicrochipNumber,
            request.RegistrationNumber);

        horse.RecordOwnership(previousOwnerId: null, newOwnerId: request.OwnerId, notes: "Initial registration.");
        horse.CreatedBy = actingUserId;

        _horseRepository.Add(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await _horseRepository.GetByIdAsync(horse.Id, cancellationToken);
        return _mapper.Map<HorseDto>(created);
    }
}
