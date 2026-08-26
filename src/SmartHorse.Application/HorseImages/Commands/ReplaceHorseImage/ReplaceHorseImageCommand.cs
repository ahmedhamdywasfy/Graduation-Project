using MediatR;
using SmartHorse.Application.HorseImages.DTOs;

namespace SmartHorse.Application.HorseImages.Commands.ReplaceHorseImage;

public record ReplaceHorseImageCommand(Guid HorseId, Guid ImageId, Stream Content, string FileName, string ContentType) : IRequest<HorseGalleryImageDto>;
