namespace SmartHorse.Application.Horses.DTOs;

public class HorseImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
