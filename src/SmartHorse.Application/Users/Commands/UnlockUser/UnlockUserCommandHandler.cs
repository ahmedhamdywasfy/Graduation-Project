using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Users.Commands.UnlockUser;

public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public UnlockUserCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.UnlockByAdministrator();
        _userRepository.Update(user);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.AccountUnlockedByAdministrator, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
