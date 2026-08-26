namespace SmartHorse.Application.Horses.DTOs;

/// <summary>Lightweight projection for list/grid views (GetAllHorses, SearchHorses) — Person 2 Sprint 1 §10.</summary>
public class HorseSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string GenderName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? PrimaryImageUrl { get; set; }
}
