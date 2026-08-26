using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Queries.SearchHorses;

public class SearchHorsesQueryHandler : IRequestHandler<SearchHorsesQuery, PagedHorseListDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public SearchHorsesQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<PagedHorseListDto> Handle(SearchHorsesQuery request, CancellationToken cancellationToken)
    {
        var criteria = new HorseSearchCriteria
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            BreedId = request.BreedId,
            ColorId = request.ColorId,
            GenderId = request.GenderId,
            StatusId = request.StatusId,
            MinAgeYears = request.MinAgeYears,
            MaxAgeYears = request.MaxAgeYears,
            MinWeight = request.MinWeight,
            MaxWeight = request.MaxWeight,
            MinHeight = request.MinHeight,
            MaxHeight = request.MaxHeight,
            BirthDateFrom = request.BirthDateFrom,
            BirthDateTo = request.BirthDateTo,
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
