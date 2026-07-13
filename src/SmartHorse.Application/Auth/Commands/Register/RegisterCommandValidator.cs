using FluentValidation;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    // Publicly self-registerable roles. Administrator is deliberately excluded —
    // it is only ever assigned via the seeded account or the Admin module (v0.2 Section 2).
    private static readonly string[] AllowedSelfRegisterRoles =
    {
        Role.Names.Owner,
        Role.Names.Veterinarian,
        Role.Names.Trainer,
        Role.Names.Worker,
        Role.Names.Buyer
    };

    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);

        // Password policy per v0.2 Security Review, Section 8.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.RequestedRole)
            .NotEmpty().WithMessage("A role is required.")
            .Must(role => AllowedSelfRegisterRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedSelfRegisterRoles)}.");
    }
}
