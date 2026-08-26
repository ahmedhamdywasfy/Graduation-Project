namespace SmartHorse.Application.HorseImages.DTOs;

/// <summary>Request body for PUT /api/v1/horses/{horseId}/images/reorder — the full ordered list of image Ids.</summary>
public record ReorderHorseImagesDto(IReadOnlyList<Guid> OrderedImageIds);
