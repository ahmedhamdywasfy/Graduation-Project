using FluentValidation;

namespace SmartHorse.Application.Users.Queries.GetUserAuditLogs;

public class GetUserAuditLogsQueryValidator : AbstractValidator<GetUserAuditLogsQuery>
{
    public GetUserAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.ToUtc)
            .GreaterThanOrEqualTo(x => x.FromUtc)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("toUtc must not be earlier than fromUtc.");
    }
}
