namespace SmartHorse.Application.Lineage.DTOs;

public class ChildHorseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public string GenderName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
}
