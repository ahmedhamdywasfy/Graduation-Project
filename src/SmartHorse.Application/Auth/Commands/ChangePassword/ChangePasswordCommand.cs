using MediatR;

namespace SmartHorse.Application.Auth.Commands.ChangePassword;

/// <summary>UserId is taken from the authenticated caller (ICurrentUserService), never from the request body.</summary>
public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest;
