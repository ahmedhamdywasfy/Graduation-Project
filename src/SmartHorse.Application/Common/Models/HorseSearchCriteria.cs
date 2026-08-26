namespace SmartHorse.Application.Common.Models;

/// <summary>
/// Search/filter/sort/pagination parameters for Horse listing (Person 2 Sprint 1
/// §6 — Search &amp; Filtering), mirroring the existing <c>UserSearchCriteria</c>
/// pattern from Person 1 Sprint 2. Used by both GetAllHorsesQuery (criteria left
/// at its filter defaults — pagination/sort only) and SearchHorsesQuery (full
/// criteria), so IHorseRepository.GetPagedAsync has a single implementation.
/// </summary>
public class HorseSearchCriteria
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Matches against Name, MicrochipNumber, or RegistrationNumber (case-insensitive, contains).</summary>
    public string? SearchTerm { get; init; }

    public int? BreedId { get; init; }
    public int? ColorId { get; init; }
    public int? GenderId { get; init; }
    public int? StatusId { get; init; }

    public int? MinAgeYears { get; init; }
    public int? MaxAgeYears { get; init; }
    public decimal? MinWeight { get; init; }
    public decimal? MaxWeight { get; init; }
    public decimal? MinHeight { get; init; }
    public decimal? MaxHeight { get; init; }
    public DateTime? BirthDateFrom { get; init; }
    public DateTime? BirthDateTo { get; init; }

    /// <summary>One of: name, createdat, age (case-insensitive). Defaults to name.</summary>
    public string SortBy { get; init; } = "name";

    public bool SortDescending { get; init; }
}
