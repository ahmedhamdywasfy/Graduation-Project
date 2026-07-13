using MediatR;
using SmartHorse.Application.Auth.DTOs;

namespace SmartHorse.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
