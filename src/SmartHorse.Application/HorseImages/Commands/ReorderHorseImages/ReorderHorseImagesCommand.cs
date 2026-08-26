using MediatR;

namespace SmartHorse.Application.HorseImages.Commands.ReorderHorseImages;

public record ReorderHorseImagesCommand(Guid HorseId, IReadOnlyList<Guid> OrderedImageIds) : IRequest;
