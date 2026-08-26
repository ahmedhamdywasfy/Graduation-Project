using FluentValidation;

namespace SmartHorse.Application.Lineage.Commands.SetLineage;

public class SetLineageCommandValidator : AbstractValidator<SetLineageCommand>
{
    public SetLineageCommandValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);

        RuleFor(x => x)
            .Must(x => x.FatherId.HasValue || x.MotherId.HasValue)
            .WithMessage("At least one of fatherId or motherId must be supplied.");

        RuleFor(x => x)
            .Must(x => !x.FatherId.HasValue || !x.MotherId.HasValue || x.FatherId != x.MotherId)
            .WithMessage("Father and mother cannot be the same horse.");
    }
}
