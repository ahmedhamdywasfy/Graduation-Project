using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Lineage.Queries.GetFamilyTree;

/// <summary>
/// Builds a family tree by repeatedly calling
/// <see cref="IHorseRepository.GetByIdWithParentsAsync"/> one generation at a
/// time (Sprint 2 §4). Deliberately not a single deep EF Core Include chain —
/// a full binary ancestor tree needs 2^depth-1 branches, which would need a
/// combinatorial Include path per branch; N small single-level queries (bounded
/// by MaxGenerations, itself capped at Horse.MaxLineageDepth) is simpler and
/// keeps tree-walking logic in the Application layer rather than the repository.
/// </summary>
public class GetFamilyTreeQueryHandler : IRequestHandler<GetFamilyTreeQuery, FamilyTreeNodeDto>
{
    private readonly IHorseRepository _horseRepository;

    public GetFamilyTreeQueryHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<FamilyTreeNodeDto> Handle(GetFamilyTreeQuery request, CancellationToken cancellationToken)
    {
        var root = await _horseRepository.GetByIdWithParentsAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        var maxGenerations = Math.Min(request.MaxGenerations, Horse.MaxLineageDepth);
        return await BuildNodeAsync(root, generation: 0, maxGenerations, cancellationToken);
    }

    private async Task<FamilyTreeNodeDto> BuildNodeAsync(Horse horse, int generation, int maxGenerations, CancellationToken cancellationToken)
    {
        var node = new FamilyTreeNodeDto
        {
            Id = horse.Id,
            Name = horse.Name,
            BreedName = horse.Breed.Name,
            GenderName = horse.Gender.Name,
            Generation = generation
        };

        if (generation >= maxGenerations)
        {
            return node;
        }

        if (horse.FatherId.HasValue)
        {
            var father = await _horseRepository.GetByIdWithParentsAsync(horse.FatherId.Value, cancellationToken);
            if (father is not null)
            {
                node.Father = await BuildNodeAsync(father, generation + 1, maxGenerations, cancellationToken);
            }
        }

        if (horse.MotherId.HasValue)
        {
            var mother = await _horseRepository.GetByIdWithParentsAsync(horse.MotherId.Value, cancellationToken);
            if (mother is not null)
            {
                node.Mother = await BuildNodeAsync(mother, generation + 1, maxGenerations, cancellationToken);
            }
        }

        return node;
    }
}
