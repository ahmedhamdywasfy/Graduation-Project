using MediatR;
using SmartHorse.Application.Auth.DTOs;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Rotates refresh tokens on every use (v0.2 Security Review, Section 8): the
/// presented token is validated, immediately revoked and marked as replaced, and
/// a brand-new access/refresh token pair is issued. If a token that was already
/// marked "replaced" is presented again, this is treated as reuse of a stolen
/// token, and the user's entire refresh token chain is revoked as a precaution
/// (Sprint 2 §4 — Detect Token Reuse / Revoke Compromised Sessions), with every
/// outcome recorded to the audit trail (Sprint 2 §6).
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtService jwtService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = _jwtService.HashRefreshToken(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(presentedHash, cancellationToken);

        if (existingToken is null)
        {
            throw new InvalidRefreshTokenException();
        }

        // Reuse of an already-rotated token is a theft signal: revoke the whole chain.
        if (existingToken.ReplacedByTokenId.HasValue || existingToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId, cancellationToken);

            _auditLogRepository.Add(new AuditLog(
                existingToken.UserId, AuditAction.RefreshTokenReuseDetected, _requestContext.IpAddress, _requestContext.UserAgent,
                "Presented refresh token had already been rotated or revoked — all sessions revoked as a precaution."));

            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        if (existingToken.IsExpired)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.GetByIdWithRolesAsync(existingToken.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        if (!user.IsActive)
        {
            throw new AccountInactiveException();
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshValue = _jwtService.GenerateRefreshTokenValue();
        var newRefreshHash = _jwtService.HashRefreshToken(newRefreshValue);
        var newRefreshExpiry = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifetime);

        // Same root cause as LoginCommandHandler: `user` here is already
        // tracked (loaded via GetByIdWithRolesAsync), so the new RefreshToken
        // returned by IssueRefreshToken() must be registered explicitly or EF
        // Core tracks it as Unchanged (silently skipped on SaveChanges) instead
        // of Added — see the comment in LoginCommandHandler for the full
        // explanation.
        var newToken = user.IssueRefreshToken(newRefreshHash, newRefreshExpiry, _requestContext.IpAddress, _requestContext.UserAgent);
        _refreshTokenRepository.Add(newToken);
        existingToken.MarkReplacedBy(newToken.Id);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.RefreshTokenUsed, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles,
            AccessToken = newAccessToken,
            AccessTokenExpiresAtUtc = DateTime.UtcNow.Add(_jwtService.AccessTokenLifetime),
            RefreshToken = newRefreshValue,
            RefreshTokenExpiresAtUtc = newRefreshExpiry
        };
    }
}
