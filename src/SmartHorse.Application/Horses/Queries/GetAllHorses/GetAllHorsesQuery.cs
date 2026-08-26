using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Queries.GetAllHorses;

/// <summary>
/// Unfiltered paginated horse listing (Person 2 Sprint 1 §5 — "Get All Horses").
/// For filtered/keyword search, see <c>SearchHorsesQuery</c>; both ultimately
/// call the same <c>IHorseRepository.GetPagedAsync</c>, so there is no
/// duplicated query logic between the two.
/// </summary>
public record GetAllHorsesQuery(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "name",
    bool SortDescending = false) : IRequest<PagedHorseListDto>;
