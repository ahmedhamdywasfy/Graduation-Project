using MediatR;

namespace SmartHorse.Application.HorseImages.Commands.DeleteHorseImage;

public record DeleteHorseImageCommand(Guid HorseId, Guid ImageId) : IRequest;
