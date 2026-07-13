namespace SmartHorse.Application.Users.DTOs;

public class PagedAuditLogListDto
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = Array.Empty<AuditLogDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
