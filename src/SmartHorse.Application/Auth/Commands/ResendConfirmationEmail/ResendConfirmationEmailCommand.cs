using MediatR;

namespace SmartHorse.Application.Auth.Commands.ResendConfirmationEmail;

public record ResendConfirmationEmailCommand(string Email) : IRequest;
