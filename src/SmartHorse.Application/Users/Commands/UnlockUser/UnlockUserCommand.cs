using MediatR;

namespace SmartHorse.Application.Users.Commands.UnlockUser;

/// <summary>Administrator-only (Sprint 2 §5 — Administrator Unlock Support).</summary>
public record UnlockUserCommand(Guid UserId) : IRequest;
