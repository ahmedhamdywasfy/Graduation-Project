using MediatR;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;
