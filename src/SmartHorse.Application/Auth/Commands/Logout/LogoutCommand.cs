using MediatR;

namespace SmartHorse.Application.Auth.Commands.Logout;

/// <summary>
/// Revokes a single refresh token (the one used on the current device). Also see
/// the future `/auth/logout-all-devices` endpoint from v0.2 Section 10.2, which
/// calls the same IRefreshTokenRepository.RevokeAllForUserAsync used for reuse
/// detection — recommended as a Sprint 2 addition, not implemented here.
/// </summary>
public record LogoutCommand(string RefreshToken) : IRequest;
