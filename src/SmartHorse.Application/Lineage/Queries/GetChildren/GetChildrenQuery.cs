using MediatR;
using SmartHorse.Application.Lineage.DTOs;

namespace SmartHorse.Application.Lineage.Queries.GetChildren;

public record GetChildrenQuery(Guid HorseId) : IRequest<IReadOnlyList<ChildHorseDto>>;
