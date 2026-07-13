using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Queries.GetUserAuditLogs;

public class GetUserAuditLogsQueryHandler : IRequestHandler<GetUserAuditLogsQuery, PagedAuditLogListDto>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public GetUserAuditLogsQueryHandler(IAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    public async Task<PagedAuditLogListDto> Handle(GetUserAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
            request.UserId, request.Action, request.FromUtc, request.ToUtc, request.Page, request.PageSize, cancellationToken);

        return new PagedAuditLogListDto
        {
            Items = _mapper.Map<IReadOnlyList<AuditLogDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
