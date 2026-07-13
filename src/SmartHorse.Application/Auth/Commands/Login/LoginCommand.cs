using MediatR;
using SmartHorse.Application.Auth.DTOs;

namespace SmartHorse.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
