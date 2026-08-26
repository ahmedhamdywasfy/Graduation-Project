namespace SmartHorse.Application.Lineage.DTOs;

/// <summary>
/// Request body for PUT /api/v1/horses/{horseId}/lineage. Both are optional and
/// independent — supplying only FatherId leaves the mother (if any) untouched,
/// and vice versa; omit both fields (nulls) to leave lineage unchanged (use the
/// dedicated DELETE endpoint to clear it).
/// </summary>
public record SetLineageDto(Guid? FatherId, Guid? MotherId);
