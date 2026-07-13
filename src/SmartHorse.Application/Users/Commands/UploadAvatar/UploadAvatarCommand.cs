using MediatR;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Commands.UploadAvatar;

/// <summary>UserId comes from the authenticated caller. Stream ownership/disposal belongs to the caller (controller).</summary>
public record UploadAvatarCommand(Guid UserId, Stream Content, string FileName, string ContentType) : IRequest<AvatarUploadResultDto>;
