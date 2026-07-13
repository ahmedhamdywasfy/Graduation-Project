using MediatR;

namespace SmartHorse.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Token) : IRequest;
