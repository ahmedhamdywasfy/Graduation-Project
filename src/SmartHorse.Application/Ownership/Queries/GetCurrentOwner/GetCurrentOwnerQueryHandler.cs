using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Ownership.Queries.GetCurrentOwner;

public class GetCurrentOwnerQueryHandler : IRequestHandler<GetCurrentOwnerQuery, OwnershipDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetCurrentOwnerQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<OwnershipDto> Handle(GetCurrentOwnerQuery request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithDetailsAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        return _mapper.Map<OwnershipDto>(horse);
    }
}
