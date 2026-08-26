using FluentValidation;

namespace SmartHorse.Application.Horses.Queries.GetAllHorses;

public class GetAllHorsesQueryValidator : AbstractValidator<GetAllHorsesQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "createdat", "age" };

    public GetAllHorsesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
    }
}
