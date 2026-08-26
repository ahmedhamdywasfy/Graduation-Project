using AutoMapper;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Horses.Mappings;

/// <summary>
/// AutoMapper profile for the Horses module (Person 2 Sprint 1), kept in its own
/// module folder per the convention already stated in Person 1's
/// <c>MappingProfile</c> doc comment ("Later sprints add their own profiles in
/// their respective module folders"). Auto-discovered by the existing
/// <c>services.AddAutoMapper(assembly)</c> call in
/// <c>SmartHorse.Application.DependencyInjection</c> — no DI changes needed.
/// </summary>
public class HorseMappingProfile : Profile
{
    public HorseMappingProfile()
    {
        CreateMap<Horse, HorseSummaryDto>()
            .ForMember(d => d.BreedName, o => o.MapFrom(s => s.Breed.Name))
            .ForMember(d => d.ColorName, o => o.MapFrom(s => s.Color.Name))
            .ForMember(d => d.GenderName, o => o.MapFrom(s => s.Gender.Name))
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.Name))
            .ForMember(d => d.Age, o => o.MapFrom(s => s.Age))
            .ForMember(d => d.PrimaryImageUrl, o => o.MapFrom(s =>
                s.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                ?? s.Images.Select(i => i.ImageUrl).FirstOrDefault()));

        CreateMap<Horse, HorseDto>()
            .ForMember(d => d.BreedName, o => o.MapFrom(s => s.Breed.Name))
            .ForMember(d => d.ColorName, o => o.MapFrom(s => s.Color.Name))
            .ForMember(d => d.GenderName, o => o.MapFrom(s => s.Gender.Name))
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.Name))
            .ForMember(d => d.Age, o => o.MapFrom(s => s.Age));

        CreateMap<Horse, HorseDetailsDto>()
            .ForMember(d => d.BreedName, o => o.MapFrom(s => s.Breed.Name))
            .ForMember(d => d.ColorName, o => o.MapFrom(s => s.Color.Name))
            .ForMember(d => d.GenderName, o => o.MapFrom(s => s.Gender.Name))
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.Name))
            .ForMember(d => d.Age, o => o.MapFrom(s => s.Age))
            .ForMember(d => d.CurrentOwnerName, o => o.MapFrom(s => s.CurrentOwner.FullName))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
            .ForMember(d => d.OwnershipHistory, o => o.MapFrom(s => s.OwnershipHistory.OrderByDescending(h => h.ChangedAtUtc)));

        CreateMap<HorseImage, HorseImageDto>();

        CreateMap<OwnershipHistory, OwnershipHistoryDto>()
            .ForMember(d => d.PreviousOwnerName, o => o.MapFrom(s => s.PreviousOwner != null ? s.PreviousOwner.FullName : null))
            .ForMember(d => d.NewOwnerName, o => o.MapFrom(s => s.NewOwner.FullName));
    }
}
