using FluentValidation;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Horses.Commands.UpdateHorse;

public class UpdateHorseCommandValidator : AbstractValidator<UpdateHorseCommand>
{
    public UpdateHorseCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Horse name is required.")
            .MaximumLength(200);

        RuleFor(x => x.BreedId).GreaterThan(0).WithMessage("A valid breed is required.");
        RuleFor(x => x.ColorId).GreaterThan(0).WithMessage("A valid color is required.");
        RuleFor(x => x.GenderId).GreaterThan(0).WithMessage("A valid gender is required.");
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("A valid status is required.");

        RuleFor(x => x.Weight)
            .InclusiveBetween(Horse.MinWeightKg, Horse.MaxWeightKg)
            .WithMessage($"Weight must be between {Horse.MinWeightKg} and {Horse.MaxWeightKg} kg.");

        RuleFor(x => x.Height)
            .InclusiveBetween(Horse.MinHeightCm, Horse.MaxHeightCm)
            .WithMessage($"Height must be between {Horse.MinHeightCm} and {Horse.MaxHeightCm} cm.");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth date is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date).WithMessage("Birth date cannot be in the future.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.MicrochipNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.MicrochipNumber));

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.RegistrationNumber));
    }
}
