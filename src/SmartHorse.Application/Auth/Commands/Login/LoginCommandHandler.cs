using MediatR;
using SmartHorse.Application.Auth.DTOs;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.Login;

/// <summary>
/// Validates credentials, applies the account lockout policy (v0.2 Security
/// Review, Section 8), records an audit entry for every outcome (Sprint 2 §6),
/// and issues a fresh access/refresh token pair on success.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Deliberately generic failure — never reveal whether the email exists.
        if (user is null)
        {
            _auditLogRepository.Add(new AuditLog(
                null, AuditAction.LoginFailed, _requestContext.IpAddress, _requestContext.UserAgent, $"Unknown email: {normalizedEmail}"));
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (user.IsLockedOut)
        {
            _auditLogRepository.Add(new AuditLog(
                user.Id, AuditAction.LoginFailed, _requestContext.IpAddress, _requestContext.UserAgent, "Account locked"));
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AccountLockedException(user.LockedUntilUtc!.Value);
        }

        if (!user.IsActive)
        {
            _auditLogRepository.Add(new AuditLog(
                user.Id, AuditAction.LoginFailed, _requestContext.IpAddress, _requestContext.UserAgent, "Account inactive"));
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AccountInactiveException();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();

            var wasJustLockedOut = user.IsLockedOut;
            _auditLogRepository.Add(new AuditLog(
                user.Id, AuditAction.LoginFailed, _requestContext.IpAddress, _requestContext.UserAgent, "Wrong password"));

            if (wasJustLockedOut)
            {
                _auditLogRepository.Add(new AuditLog(
                    user.Id, AuditAction.AccountLockedOut, _requestContext.IpAddress, _requestContext.UserAgent,
                    $"Locked until {user.LockedUntilUtc:u} after {User.MaxFailedLoginAttempts} failed attempts"));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        user.RecordSuccessfulLogin();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwtService.GenerateRefreshTokenValue();
        var refreshTokenHash = _jwtService.HashRefreshToken(refreshTokenValue);
        var refreshTokenExpiry = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifetime);

        // IssueRefreshToken() only appends the new token to User's in-memory
        // navigation collection — it does NOT register it with the DbContext.
        // `user` here is an ALREADY-TRACKED entity (loaded via GetByEmailAsync,
        // not via _userRepository.Add()), so EF Core's automatic change
        // detection discovers this new RefreshToken purely through graph
        // traversal. Per Microsoft's documented EF Core behavior, a new entity
        // reached this way — whose primary key is already non-default (it
        // always is here, due to BaseEntity's `Guid.NewGuid()` field
        // initializer) — gets tracked as Unchanged instead of Added, so
        // SaveChanges silently skips inserting it. Registering it explicitly
        // here is required for it to actually be persisted.
        var refreshToken = user.IssueRefreshToken(refreshTokenHash, refreshTokenExpiry, _requestContext.IpAddress, _requestContext.UserAgent);
        _refreshTokenRepository.Add(refreshToken);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.Login, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles,
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = DateTime.UtcNow.Add(_jwtService.AccessTokenLifetime),
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiry
        };
    }
}
