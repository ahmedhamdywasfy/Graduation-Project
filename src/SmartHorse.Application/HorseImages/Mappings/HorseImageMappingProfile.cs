using AutoMapper;
using SmartHorse.Application.HorseImages.DTOs;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.HorseImages.Mappings;

public class HorseImageMappingProfile : Profile
{
    public HorseImageMappingProfile()
    {
        CreateMap<HorseImage, HorseGalleryImageDto>();
    }
}
