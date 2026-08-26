namespace SmartHorse.Application.Horses.DTOs;

public class OwnershipHistoryDto
{
    public Guid Id { get; set; }
    public Guid? PreviousOwnerId { get; set; }
    public string? PreviousOwnerName { get; set; }
    public Guid NewOwnerId { get; set; }
    public string NewOwnerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
