using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Ownership.Queries.GetOwnershipHistory;

public class GetOwnershipHistoryQueryHandler : IRequestHandler<GetOwnershipHistoryQuery, IReadOnlyList<OwnershipHistoryRecordDto>>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IOwnershipHistoryRepository _ownershipHistoryRepository;
    private readonly IMapper _mapper;

    public GetOwnershipHistoryQueryHandler(
        IHorseRepository horseRepository,
        IOwnershipHistoryRepository ownershipHistoryRepository,
        IMapper mapper)
    {
        _horseRepository = horseRepository;
        _ownershipHistoryRepository = ownershipHistoryRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<OwnershipHistoryRecordDto>> Handle(GetOwnershipHistoryQuery request, CancellationToken cancellationToken)
    {
        if (await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Horse), request.HorseId);
        }

        var records = await _ownershipHistoryRepository.GetByHorseIdAsync(request.HorseId, includeDeleted: false, cancellationToken);
        return _mapper.Map<IReadOnlyList<OwnershipHistoryRecordDto>>(records);
    }
}
