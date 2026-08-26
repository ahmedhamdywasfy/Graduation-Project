namespace SmartHorse.Application.Horses.DTOs;

/// <summary>Standard list envelope, consistent with v0.1 §25 and the existing PagedUserListDto.</summary>
public class PagedHorseListDto
{
    public IReadOnlyList<HorseSummaryDto> Items { get; set; } = Array.Empty<HorseSummaryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
