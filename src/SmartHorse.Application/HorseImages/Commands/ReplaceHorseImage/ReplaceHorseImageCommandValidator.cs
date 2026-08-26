using FluentValidation;

namespace SmartHorse.Application.HorseImages.Commands.ReplaceHorseImage;

public class ReplaceHorseImageCommandValidator : AbstractValidator<ReplaceHorseImageCommand>
{
    public ReplaceHorseImageCommandValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);
        RuleFor(x => x.ImageId).NotEqual(Guid.Empty);
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
