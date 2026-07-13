using MediatR;
using SmartHorse.Application.Auth.DTOs;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.Register;

/// <summary>
/// Creates a new user with a hashed password and their requested role, issues an
/// email confirmation token (Sprint 2 §1), and immediately issues an access +
/// refresh token pair (auto-login on register — a common and acceptable UX
/// choice; email confirmation is tracked separately via <see cref="User.EmailConfirmed"/>
/// and can gate sensitive actions later without blocking this initial login).
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ISecureTokenGenerator secureTokenGenerator,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _secureTokenGenerator = secureTokenGenerator;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException(normalizedEmail);
        }

        var role = await _roleRepository.GetByNameAsync(request.RequestedRole, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RequestedRole);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(request.FullName, normalizedEmail, passwordHash, request.PhoneNumber);
        user.AssignRole(role);

        var accessToken = _jwtService.GenerateAccessToken(user, new[] { role.Name });
        var refreshTokenValue = _jwtService.GenerateRefreshTokenValue();
        var refreshTokenHash = _jwtService.HashRefreshToken(refreshTokenValue);
        var refreshTokenExpiry = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifetime);

        // Not strictly required here — `user` is a brand-new aggregate, and
        // `_userRepository.Add(user)` below cascades EntityState.Added to its
        // whole object graph, including this token. Made explicit anyway so
        // this handler doesn't silently depend on that cascade behavior, and
        // stays consistent with Login/RefreshToken, where it IS required
        // (see the comment in LoginCommandHandler for why).
        var refreshToken = user.IssueRefreshToken(refreshTokenHash, refreshTokenExpiry, _requestContext.IpAddress, _requestContext.UserAgent);
        _refreshTokenRepository.Add(refreshToken);

        var rawConfirmationToken = _secureTokenGenerator.GenerateToken();
        var confirmationTokenHash = _secureTokenGenerator.HashToken(rawConfirmationToken);
        user.SetEmailConfirmationToken(confirmationTokenHash, DateTime.UtcNow.Add(ConfirmationTokenLifetime));

        _userRepository.Add(user);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.Register, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, rawConfirmationToken, cancellationToken);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = new[] { role.Name },
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = DateTime.UtcNow.Add(_jwtService.AccessTokenLifetime),
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiry
        };
    }
}
