using MediatR;

namespace SmartHorse.Application.Users.Commands.DeactivateUser;

/// <summary>Administrator-only (v0.2 Section 2.3 — POST /admin/users/{id}/deactivate).</summary>
public record DeactivateUserCommand(Guid UserId) : IRequest;
