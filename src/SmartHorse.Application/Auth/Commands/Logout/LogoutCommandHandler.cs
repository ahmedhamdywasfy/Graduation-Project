using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _jwtService.HashRefreshToken(request.RefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);

        // Idempotent: logging out with an already-invalid token is not an error.
        if (token is not null && !token.IsRevoked)
        {
            token.Revoke();

            _auditLogRepository.Add(new AuditLog(
                token.UserId, AuditAction.Logout, _requestContext.IpAddress, _requestContext.UserAgent));

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
