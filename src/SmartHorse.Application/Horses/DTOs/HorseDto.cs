namespace SmartHorse.Application.Horses.DTOs;

/// <summary>Standard single-resource shape, returned by Create/Update — Person 2 Sprint 1 §10.</summary>
public class HorseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;

    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;

    public int GenderId { get; set; }
    public string GenderName { get; set; } = string.Empty;

    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;

    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }

    public string? Description { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? RegistrationNumber { get; set; }

    public Guid CurrentOwnerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
