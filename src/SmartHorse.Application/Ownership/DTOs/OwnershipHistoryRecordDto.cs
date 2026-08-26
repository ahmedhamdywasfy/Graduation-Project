namespace SmartHorse.Application.Ownership.DTOs;

/// <summary>
/// A single ownership stint, including the Sprint 2 §1 Purchase/Sale Date pair.
/// Distinct from (and supersedes, for Ownership-module endpoints) the
/// Horses-module <c>OwnershipHistoryDto</c> introduced in Sprint 1, which
/// remains unchanged for backward compatibility with HorseDetailsDto.
/// </summary>
public class OwnershipHistoryRecordDto
{
    public Guid Id { get; set; }
    public Guid HorseId { get; set; }
    public Guid? PreviousOwnerId { get; set; }
    public string? PreviousOwnerName { get; set; }
    public Guid NewOwnerId { get; set; }
    public string NewOwnerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? SaleDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
