using FluentValidation;

namespace SmartHorse.Application.HorseImages.Commands.ReorderHorseImages;

public class ReorderHorseImagesCommandValidator : AbstractValidator<ReorderHorseImagesCommand>
{
    public ReorderHorseImagesCommandValidator()
    {
        RuleFor(x => x.HorseId).NotEqual(Guid.Empty);
        RuleFor(x => x.OrderedImageIds).NotEmpty().WithMessage("At least one image Id is required.");
    }
}
