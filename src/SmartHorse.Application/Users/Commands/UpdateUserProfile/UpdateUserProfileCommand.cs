using MediatR;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Commands.UpdateUserProfile;

/// <summary>A user editing their own profile — UserId is taken from the authenticated caller, not the request body.</summary>
public record UpdateUserProfileCommand(Guid UserId, string FullName, string? PhoneNumber) : IRequest<UserDto>;
