using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Queries.GetAllHorses;

public class GetAllHorsesQueryHandler : IRequestHandler<GetAllHorsesQuery, PagedHorseListDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetAllHorsesQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<PagedHorseListDto> Handle(GetAllHorsesQuery request, CancellationToken cancellationToken)
    {
        var criteria = new HorseSearchCriteria
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        var (items, totalCount) = await _horseRepository.GetPagedAsync(criteria, cancellationToken);

        return new PagedHorseListDto
        {
            Items = _mapper.Map<IReadOnlyList<HorseSummaryDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
