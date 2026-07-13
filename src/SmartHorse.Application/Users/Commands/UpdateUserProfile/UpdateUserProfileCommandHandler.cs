using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Users.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IRequestContextAccessor requestContext,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _requestContext = requestContext;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.UpdateProfile(request.FullName, request.PhoneNumber);
        _userRepository.Update(user);

        _auditLogRepository.Add(new AuditLog(
            user.Id, AuditAction.ProfileUpdated, _requestContext.IpAddress, _requestContext.UserAgent));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
