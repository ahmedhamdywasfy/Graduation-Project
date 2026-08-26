namespace SmartHorse.Application.Horses.DTOs;

/// <summary>Request body for POST /api/v1/horses — Person 2 Sprint 1 §10.</summary>
public record CreateHorseDto(
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
    Guid OwnerId);
