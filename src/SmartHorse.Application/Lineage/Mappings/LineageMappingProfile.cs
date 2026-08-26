using AutoMapper;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Lineage.Mappings;

public class LineageMappingProfile : Profile
{
    public LineageMappingProfile()
    {
        CreateMap<Horse, LineageDto>()
            .ForMember(d => d.HorseId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.HorseName, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.FatherName, o => o.MapFrom(s => s.Father != null ? s.Father.Name : null))
            .ForMember(d => d.MotherName, o => o.MapFrom(s => s.Mother != null ? s.Mother.Name : null));

        CreateMap<Horse, ChildHorseDto>()
            .ForMember(d => d.BreedName, o => o.MapFrom(s => s.Breed.Name))
            .ForMember(d => d.GenderName, o => o.MapFrom(s => s.Gender.Name));

        // FamilyTreeNodeDto is built recursively in GetFamilyTreeQueryHandler
        // rather than via a single declarative AutoMapper map, since the
        // recursion depth/generation numbering needs handler-side state
        // (current depth) that a CreateMap ForMember expression can't carry.
    }
}
