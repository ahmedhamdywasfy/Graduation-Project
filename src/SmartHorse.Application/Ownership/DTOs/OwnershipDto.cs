namespace SmartHorse.Application.Ownership.DTOs;

/// <summary>Current-owner summary for a horse — Sprint 2 §11.</summary>
public class OwnershipDto
{
    public Guid HorseId { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string? Notes { get; set; }
}
