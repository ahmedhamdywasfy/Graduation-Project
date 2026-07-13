using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Application.Auth.Commands.ResendConfirmationEmail;

/// <summary>
/// Re-issues a fresh confirmation token and re-sends the email (Sprint 2 §1).
/// Always no-ops silently for unknown/already-confirmed accounts — same
/// information-disclosure guard used by ForgotPassword (v0.2 §8).
/// </summary>
public class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand>
{
    private static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public ResendConfirmationEmailCommandHandler(
        IUserRepository userRepository,
        ISecureTokenGenerator tokenGenerator,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.EmailConfirmed || !user.IsActive)
        {
            return;
        }

        var rawToken = _tokenGenerator.GenerateToken();
        var tokenHash = _tokenGenerator.HashToken(rawToken);

        user.SetEmailConfirmationToken(tokenHash, DateTime.UtcNow.Add(ConfirmationTokenLifetime));
        _userRepository.Update(user);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.EmailConfirmationRequested, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, rawToken, cancellationToken);
    }
}
