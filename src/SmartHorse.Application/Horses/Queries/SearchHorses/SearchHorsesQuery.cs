using MediatR;
using SmartHorse.Application.Horses.DTOs;

namespace SmartHorse.Application.Horses.Queries.SearchHorses;

/// <summary>Filtered, keyword-searchable, sortable, paginated horse listing (Person 2 Sprint 1 §6).</summary>
public record SearchHorsesQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    int? BreedId = null,
    int? ColorId = null,
    int? GenderId = null,
    int? StatusId = null,
    int? MinAgeYears = null,
    int? MaxAgeYears = null,
    decimal? MinWeight = null,
    decimal? MaxWeight = null,
    decimal? MinHeight = null,
    decimal? MaxHeight = null,
    DateTime? BirthDateFrom = null,
    DateTime? BirthDateTo = null,
    string SortBy = "name",
    bool SortDescending = false) : IRequest<PagedHorseListDto>;
