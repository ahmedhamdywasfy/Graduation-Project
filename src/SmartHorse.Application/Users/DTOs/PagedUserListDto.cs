namespace SmartHorse.Application.Users.DTOs;

/// <summary>
/// Standard list envelope, consistent with v0.1 Section 25 (API Design Strategy —
/// pagination for all list endpoints).
/// </summary>
public class PagedUserListDto
{
    public IReadOnlyList<UserDto> Items { get; set; } = Array.Empty<UserDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
