using AutoMapper;
using SmartHorse.Application.Users.DTOs;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Mappings;

/// <summary>
/// Central AutoMapper profile for Sprint 1+2 (Identity/User Management). Later
/// sprints add their own profiles in their respective module folders rather than
/// growing this one indefinitely.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name)));

        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action.ToString()));
    }
}
