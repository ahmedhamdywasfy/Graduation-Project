using MediatR;

namespace SmartHorse.Application.HorseImages.Commands.SetMainHorseImage;

public record SetMainHorseImageCommand(Guid HorseId, Guid ImageId) : IRequest;
