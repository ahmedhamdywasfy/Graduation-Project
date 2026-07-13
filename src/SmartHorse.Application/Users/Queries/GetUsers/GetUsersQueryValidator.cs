using FluentValidation;

namespace SmartHorse.Application.Users.Queries.GetUsers;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    private static readonly string[] AllowedSortFields = { "fullname", "email", "createdat" };

    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(sortBy => AllowedSortFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.CreatedToUtc)
            .GreaterThanOrEqualTo(x => x.CreatedFromUtc)
            .When(x => x.CreatedFromUtc.HasValue && x.CreatedToUtc.HasValue)
            .WithMessage("createdToUtc must not be earlier than createdFromUtc.");
    }
}
