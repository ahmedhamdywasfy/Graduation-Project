using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Users.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Users.Commands.UploadAvatar;

/// <summary>
/// Secure avatar upload (Sprint 2 §3, §9 — Secure File Upload). Content-type and
/// size validation happen inside <see cref="IFileStorageService"/> so every
/// caller of that abstraction gets the same enforcement, not just this handler.
/// </summary>
public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, AvatarUploadResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;

    public UploadAvatarCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
    }

    public async Task<AvatarUploadResultDto> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var avatarUrl = await _fileStorageService.SaveAvatarAsync(
            request.UserId, request.Content, request.FileName, request.ContentType, cancellationToken);

        user.SetAvatarUrl(avatarUrl);
        _userRepository.Update(user);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.AvatarUpdated, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AvatarUploadResultDto { AvatarUrl = avatarUrl };
    }
}
