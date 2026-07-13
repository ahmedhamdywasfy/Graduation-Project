namespace SmartHorse.Application.Common.Models;

/// <summary>
/// Search/sort/filter parameters for the Administrator user listing (Sprint 2 §7 —
/// User Search). Grouped into one type instead of a growing parameter list on
/// <c>IUserRepository.GetPagedAsync</c>, so adding another filter later is a
/// one-file change.
/// </summary>
public class UserSearchCriteria
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Matches against FullName or Email (case-insensitive, contains).</summary>
    public string? SearchTerm { get; init; }

    public string? RoleFilter { get; init; }

    public bool? IsActive { get; init; }

    public DateTime? CreatedFromUtc { get; init; }
    public DateTime? CreatedToUtc { get; init; }

    /// <summary>One of: fullName, email, createdAt (case-insensitive). Defaults to fullName.</summary>
    public string SortBy { get; init; } = "fullName";

    public bool SortDescending { get; init; }
}
