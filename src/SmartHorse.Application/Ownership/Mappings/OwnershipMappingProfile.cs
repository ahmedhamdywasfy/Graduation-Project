using AutoMapper;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Ownership.Mappings;

public class OwnershipMappingProfile : Profile
{
    public OwnershipMappingProfile()
    {
        CreateMap<OwnershipHistory, OwnershipHistoryRecordDto>()
            .ForMember(d => d.PreviousOwnerName, o => o.MapFrom(s => s.PreviousOwner != null ? s.PreviousOwner.FullName : null))
            .ForMember(d => d.NewOwnerName, o => o.MapFrom(s => s.NewOwner.FullName))
            .ForMember(d => d.PurchaseDate, o => o.MapFrom(s => s.ChangedAtUtc));

        CreateMap<Horse, OwnershipDto>()
            .ForMember(d => d.HorseId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.OwnerId, o => o.MapFrom(s => s.CurrentOwnerId))
            .ForMember(d => d.OwnerName, o => o.MapFrom(s => s.CurrentOwner.FullName))
            .ForMember(d => d.PurchaseDate, o => o.MapFrom(s =>
                s.OwnershipHistory.Where(h => h.IsActive).Select(h => h.ChangedAtUtc).FirstOrDefault()))
            .ForMember(d => d.Notes, o => o.MapFrom(s =>
                s.OwnershipHistory.Where(h => h.IsActive).Select(h => h.Notes).FirstOrDefault()));
    }
}
