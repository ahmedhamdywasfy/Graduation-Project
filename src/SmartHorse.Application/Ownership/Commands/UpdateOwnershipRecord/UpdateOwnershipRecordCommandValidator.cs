using FluentValidation;

namespace SmartHorse.Application.Ownership.Commands.UpdateOwnershipRecord;

public class UpdateOwnershipRecordCommandValidator : AbstractValidator<UpdateOwnershipRecordCommand>
{
    public UpdateOwnershipRecordCommandValidator()
    {
        RuleFor(x => x.RecordId).NotEqual(Guid.Empty);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.PurchaseDate).NotEmpty();
        RuleFor(x => x.SaleDate)
            .GreaterThanOrEqualTo(x => x.PurchaseDate)
            .When(x => x.SaleDate.HasValue)
            .WithMessage("saleDate must not be earlier than purchaseDate.");
    }
}
