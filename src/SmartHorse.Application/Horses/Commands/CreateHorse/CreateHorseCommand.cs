using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Commands.CreateHorse;

public record CreateHorseCommand(
    string Name,
    int BreedId,
    int ColorId,
    int GenderId,
    int? StatusId,
    decimal Weight,
    decimal Height,
    DateTime BirthDate,
    string? Description,
    string? MicrochipNumber,
    string? RegistrationNumber,
    Guid OwnerId) : IRequest<HorseDto>;
