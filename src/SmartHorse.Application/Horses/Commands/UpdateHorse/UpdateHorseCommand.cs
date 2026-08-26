using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Commands.UpdateHorse;

public record UpdateHorseCommand(
    Guid Id,
    string Name,
    int BreedId,
    int ColorId,
    int GenderId,
    int StatusId,
    decimal Weight,
    decimal Height,
    DateTime BirthDate,
    string? Description,
    string? MicrochipNumber,
    string? RegistrationNumber) : IRequest<HorseDto>;
