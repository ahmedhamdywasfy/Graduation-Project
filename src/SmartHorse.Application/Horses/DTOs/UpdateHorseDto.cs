namespace SmartHorse.Application.Horses.DTOs;

/// <summary>
/// Request body for PUT /api/v1/horses/{id} — Person 2 Sprint 1 §10. Ownership
/// is intentionally not editable here — see Horse.RecordOwnership and the
/// Implementation Report's "Future Recommendations" for a dedicated transfer flow.
/// </summary>
public record UpdateHorseDto(
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
    string? RegistrationNumber);
