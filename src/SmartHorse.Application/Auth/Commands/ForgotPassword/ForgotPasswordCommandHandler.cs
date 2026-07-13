using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Issues a short-lived password reset token and emails it to the user. Always
/// succeeds from the caller's perspective (no "email not found" response) to
/// avoid leaking which emails are registered — consistent with the login flow's
/// generic-failure approach (v0.2 Security Review, Section 8).
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _userRepository;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public ForgotPasswordCommandHandler(
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

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            // Silently no-op — do not reveal account existence/state.
            return;
        }

        var rawToken = _tokenGenerator.GenerateToken();
        var tokenHash = _tokenGenerator.HashToken(rawToken);

        user.SetPasswordResetToken(tokenHash, DateTime.UtcNow.Add(ResetTokenLifetime));

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.ForgotPasswordRequested, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, rawToken, cancellationToken);
    }
}
