using MediatR;
using SmartHorse.Application.HorseImages.DTOs;

namespace SmartHorse.Application.HorseImages.Commands.UploadHorseImage;

/// <summary>Stream ownership/disposal belongs to the caller (controller), same convention as Person 1 Sprint 2's avatar upload.</summary>
public record UploadHorseImageCommand(Guid HorseId, Stream Content, string FileName, string ContentType, bool IsPrimary) : IRequest<HorseGalleryImageDto>;
