using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Lineage.Commands.SetLineage;

/// <summary>
/// Assigns a horse's father and/or mother (Sprint 2 §3–§4). Enforces three
/// invariants beyond the entity-level self-parent guard already in
/// <see cref="Horse.SetFather"/>/<see cref="Horse.SetMother"/>:
/// 1. The candidate parent must actually be the expected gender.
/// 2. The candidate parent must not already have this horse among ITS
///    ancestors (Sprint 2 §3 — "Prevent Circular Relationships"), checked via
///    <see cref="IHorseRepository.GetAncestorIdsAsync"/>.
/// 3. The candidate parent must exist and not be soft-deleted.
/// </summary>
public class SetLineageCommandHandler : IRequestHandler<SetLineageCommand, LineageDto>
{
    // A gelding is a castrated male and cannot sire foals — excluded from valid Father genders on purpose.
    private static readonly string[] ValidFatherGenders = { "Stallion", "Colt" };
    private static readonly string[] ValidMotherGenders = { "Mare", "Filly" };

    private readonly IHorseRepository _horseRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public SetLineageCommandHandler(IHorseRepository horseRepository, IApplicationDbContext dbContext, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<LineageDto> Handle(SetLineageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        if (request.FatherId.HasValue)
        {
            await ValidateAndAssignParentAsync(horse, request.FatherId.Value, "father", ValidFatherGenders, isFather: true, cancellationToken);
        }

        if (request.MotherId.HasValue)
        {
            await ValidateAndAssignParentAsync(horse, request.MotherId.Value, "mother", ValidMotherGenders, isFather: false, cancellationToken);
        }

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var refreshed = await _horseRepository.GetByIdWithParentsAsync(horse.Id, cancellationToken);
        return _mapper.Map<LineageDto>(refreshed);
    }

    private async Task ValidateAndAssignParentAsync(
        Horse horse,
        Guid candidateParentId,
        string role,
        string[] validGenders,
        bool isFather,
        CancellationToken cancellationToken)
    {
        var candidate = await _horseRepository.GetByIdAsync(candidateParentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), candidateParentId);

        if (!validGenders.Contains(candidate.Gender?.Name, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidParentGenderException(role, validGenders[0]);
        }

        // Prevent circularity: if `horse` already appears among `candidate`'s own
        // ancestors, assigning `candidate` as `horse`'s parent would close a loop.
        var candidateAncestors = await _horseRepository.GetAncestorIdsAsync(candidateParentId, Horse.MaxLineageDepth, cancellationToken);
        if (candidateAncestors.Contains(horse.Id))
        {
            throw new CircularLineageException();
        }

        if (isFather)
        {
            horse.SetFather(candidateParentId);
        }
        else
        {
            horse.SetMother(candidateParentId);
        }
    }
}
