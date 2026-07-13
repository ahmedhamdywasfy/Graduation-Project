using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Authenticated password change (Sprint 2 §2). Verifies the current password,
/// sets the new hash, and — like ResetPassword — revokes all other active
/// refresh tokens so a change made from one device signs the account out of any
/// others, consistent with the theft-precaution stance in v0.2 Security Review §8.
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCurrentPasswordException();
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newPasswordHash);
        _userRepository.Update(user);

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.PasswordChanged, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
