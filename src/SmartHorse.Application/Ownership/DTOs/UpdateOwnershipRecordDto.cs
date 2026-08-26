namespace SmartHorse.Application.Ownership.DTOs;

/// <summary>Request body for PUT /api/v1/ownership-records/{recordId} — administrator correction of a historical record.</summary>
public record UpdateOwnershipRecordDto(string? Notes, DateTime PurchaseDate, DateTime? SaleDate);
