using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Queries.GetHorseById;

public record GetHorseByIdQuery(Guid Id) : IRequest<HorseDetailsDto>;
