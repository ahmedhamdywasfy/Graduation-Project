namespace SmartHorse.Application.Ownership.DTOs;

/// <summary>Request body for POST /api/v1/horses/{horseId}/ownership/transfer — Sprint 2 §11.</summary>
public record TransferOwnershipDto(Guid NewOwnerId, string? Notes);
