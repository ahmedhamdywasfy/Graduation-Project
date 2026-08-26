using FluentValidation;

namespace SmartHorse.Application.Ownership.Commands.TransferOwnership;

public class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);
        RuleFor(x => x.NewOwnerId).NotEqual(Guid.Empty).WithMessage("A valid new owner is required.");
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
