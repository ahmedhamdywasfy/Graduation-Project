using FluentValidation;

namespace SmartHorse.Application.Horses.Queries.SearchHorses;

public class SearchHorsesQueryValidator : AbstractValidator<SearchHorsesQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "createdat", "age" };

    public SearchHorsesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.MaxAgeYears)
            .GreaterThanOrEqualTo(x => x.MinAgeYears)
            .When(x => x.MinAgeYears.HasValue && x.MaxAgeYears.HasValue)
            .WithMessage("maxAgeYears must not be less than minAgeYears.");

        RuleFor(x => x.MaxWeight)
            .GreaterThanOrEqualTo(x => x.MinWeight)
            .When(x => x.MinWeight.HasValue && x.MaxWeight.HasValue)
            .WithMessage("maxWeight must not be less than minWeight.");

        RuleFor(x => x.MaxHeight)
            .GreaterThanOrEqualTo(x => x.MinHeight)
            .When(x => x.MinHeight.HasValue && x.MaxHeight.HasValue)
            .WithMessage("maxHeight must not be less than minHeight.");

        RuleFor(x => x.BirthDateTo)
            .GreaterThanOrEqualTo(x => x.BirthDateFrom)
            .When(x => x.BirthDateFrom.HasValue && x.BirthDateTo.HasValue)
            .WithMessage("birthDateTo must not be earlier than birthDateFrom.");
    }
}
