namespace SmartHorse.Application.HorseImages.DTOs;

public class HorseGalleryDto
{
    public Guid HorseId { get; set; }
    public IReadOnlyList<HorseGalleryImageDto> Images { get; set; } = Array.Empty<HorseGalleryImageDto>();
}
