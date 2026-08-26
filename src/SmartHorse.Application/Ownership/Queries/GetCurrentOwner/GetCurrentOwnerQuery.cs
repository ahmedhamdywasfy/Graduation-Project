using MediatR;
using SmartHorse.Application.Ownership.DTOs;

namespace SmartHorse.Application.Ownership.Queries.GetCurrentOwner;

public record GetCurrentOwnerQuery(Guid HorseId) : IRequest<OwnershipDto>;
