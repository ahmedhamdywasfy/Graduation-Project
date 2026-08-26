using FluentValidation;

namespace SmartHorse.Application.HorseImages.Commands.UploadHorseImage;

public class UploadHorseImageCommandValidator : AbstractValidator<UploadHorseImageCommand>
{
    public UploadHorseImageCommandValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
