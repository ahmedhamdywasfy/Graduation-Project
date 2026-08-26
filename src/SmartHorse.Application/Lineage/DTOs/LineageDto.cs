namespace SmartHorse.Application.Lineage.DTOs;

/// <summary>Immediate parents of a horse — Sprint 2 §11 "Get Parents".</summary>
public class LineageDto
{
    public Guid HorseId { get; set; }
    public string HorseName { get; set; } = string.Empty;

    public Guid? FatherId { get; set; }
    public string? FatherName { get; set; }

    public Guid? MotherId { get; set; }
    public string? MotherName { get; set; }
}
