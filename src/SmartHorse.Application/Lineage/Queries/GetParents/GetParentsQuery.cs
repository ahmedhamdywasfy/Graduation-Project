using MediatR;
using SmartHorse.Application.Lineage.DTOs;

namespace SmartHorse.Application.Lineage.Queries.GetParents;

public record GetParentsQuery(Guid HorseId) : IRequest<LineageDto>;
