using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Lineage.Queries.GetChildren;

public class GetChildrenQueryHandler : IRequestHandler<GetChildrenQuery, IReadOnlyList<ChildHorseDto>>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetChildrenQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ChildHorseDto>> Handle(GetChildrenQuery request, CancellationToken cancellationToken)
    {
        if (await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Horse), request.HorseId);
        }

        var children = await _horseRepository.GetChildrenAsync(request.HorseId, cancellationToken);
        return _mapper.Map<IReadOnlyList<ChildHorseDto>>(children);
    }
}
