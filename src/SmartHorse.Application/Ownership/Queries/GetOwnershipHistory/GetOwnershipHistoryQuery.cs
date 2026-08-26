using MediatR;
using SmartHorse.Application.Ownership.DTOs;

namespace SmartHorse.Application.Ownership.Queries.GetOwnershipHistory;

public record GetOwnershipHistoryQuery(Guid HorseId) : IRequest<IReadOnlyList<OwnershipHistoryRecordDto>>;
