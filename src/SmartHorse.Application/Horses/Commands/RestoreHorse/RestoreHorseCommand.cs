using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Commands.RestoreHorse;

public record RestoreHorseCommand(Guid Id) : IRequest<HorseDto>;
