using FluentValidation;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Lineage.Queries.GetFamilyTree;

public class GetFamilyTreeQueryValidator : AbstractValidator<GetFamilyTreeQuery>
{
    public GetFamilyTreeQueryValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);
        RuleFor(x => x.MaxGenerations).InclusiveBetween(1, Horse.MaxLineageDepth);
    }
}
