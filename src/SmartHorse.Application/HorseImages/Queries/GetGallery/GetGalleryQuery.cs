using MediatR;
using SmartHorse.Application.HorseImages.DTOs;

namespace SmartHorse.Application.HorseImages.Queries.GetGallery;

public record GetGalleryQuery(Guid HorseId) : IRequest<HorseGalleryDto>;
