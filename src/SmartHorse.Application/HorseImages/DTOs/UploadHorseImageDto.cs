namespace SmartHorse.Application.HorseImages.DTOs;

/// <summary>
/// Non-file metadata accompanying a multipart image upload — Sprint 2 §11. The
/// actual file arrives as an <c>IFormFile</c> model-bound parameter alongside
/// this (see HorseImagesController), consistent with how avatar upload already
/// handles multipart requests in Person 1 Sprint 2.
/// </summary>
public record UploadHorseImageDto(bool IsPrimary);
