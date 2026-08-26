using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Lineage.Queries.GetParents;

public class GetParentsQueryHandler : IRequestHandler<GetParentsQuery, LineageDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetParentsQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<LineageDto> Handle(GetParentsQuery request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithParentsAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        return _mapper.Map<LineageDto>(horse);
    }
}
