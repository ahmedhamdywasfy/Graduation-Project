using MediatR;
using SmartHorse.Application.Auth.DTOs;

namespace SmartHorse.Application.Auth.Commands.Register;

/// <summary>
/// Self-registration command. Public users register with a single role from the
/// v0.1 Section 4 role list; Administrator accounts are never created through this
/// endpoint (see DbSeeder for the seeded Administrator, and the Admin module's
/// user-management endpoints, defined in v0.2 Section 2, for creating staff accounts).
/// </summary>
public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber,
    string RequestedRole) : IRequest<AuthResponseDto>;
