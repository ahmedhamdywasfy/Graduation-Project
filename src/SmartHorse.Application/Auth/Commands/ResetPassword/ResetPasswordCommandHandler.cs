using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Verifies the reset token (hash + expiry) issued by ForgotPassword, sets the new
/// password hash, clears the reset token (single-use), and revokes all existing
/// refresh tokens for the account as a precaution (a password reset likely means
/// the account may have been compromised — v0.2 Security Review, Section 8).
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ISecureTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        var presentedTokenHash = _tokenGenerator.HashToken(request.Token);

        if (user is null || !user.IsPasswordResetTokenValid(presentedTokenHash))
        {
            throw new InvalidPasswordResetTokenException();
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newPasswordHash);
        user.ClearPasswordResetToken();

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.PasswordReset, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
