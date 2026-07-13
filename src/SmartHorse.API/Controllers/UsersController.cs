using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Users.Commands.DeactivateUser;
using SmartHorse.Application.Users.Commands.UnlockUser;
using SmartHorse.Application.Users.Commands.UpdateUserProfile;
using SmartHorse.Application.Users.Commands.UploadAvatar;
using SmartHorse.Application.Users.DTOs;
using SmartHorse.Application.Users.Queries.GetUserAuditLogs;
using SmartHorse.Application.Users.Queries.GetUserById;
using SmartHorse.Application.Users.Queries.GetUsers;
using SmartHorse.Domain.Enums;

namespace SmartHorse.API.Controllers;

/// <summary>
/// User Management endpoints (v0.1 Section 13 / v0.2 Section 2 — Administration
/// Module, User Management sub-module; Sprint 2 §3 Profile/Avatar, §5 Lockout
/// Unlock, §6 Audit Logs, §7 User Search). Self-service endpoints ("me") are
/// available to any authenticated user; listing, unlock, and audit-log endpoints
/// are Administrator-only.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private const long MaxAvatarUploadBytes = 2 * 1024 * 1024; // 2 MB — kept in sync with FileStorageSettings.MaxAvatarSizeBytes default.

    private readonly ISender _mediator;
    private readonly ICurrentUserService _currentUser;

    public UsersController(ISender mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Returns the authenticated caller's own profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await _mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Updates the authenticated caller's own profile (full name, phone number).</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> UpdateMyProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var command = new UpdateUserProfileCommand(userId, request.FullName, request.PhoneNumber);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Uploads/replaces the authenticated caller's avatar. Accepts JPEG, PNG, or
    /// WebP up to 2 MB (Sprint 2 §3, §9 — Secure File Upload; enforced again,
    /// authoritatively, inside IFileStorageService).
    /// </summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarUploadBytes)]
    [ProducesResponseType(typeof(AvatarUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvatarUploadResultDto>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        await using var stream = file.OpenReadStream();
        var command = new UploadAvatarCommand(userId, stream, file.FileName, file.ContentType);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Administrator-only: paginated, searchable, sortable, filterable user listing (Sprint 2 §7).</summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdministrator")]
    [ProducesResponseType(typeof(PagedUserListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedUserListDto>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateTime? createdFromUtc = null,
        [FromQuery] DateTime? createdToUtc = null,
        [FromQuery] string sortBy = "fullName",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(page, pageSize, search, role, isActive, createdFromUtc, createdToUtc, sortBy, sortDescending);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Administrator-only: fetch a specific user by Id.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireAdministrator")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Administrator-only: deactivates a user account and revokes all of its active sessions.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "RequireAdministrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Administrator-only: clears a lockout, independent of the automatic expiry (Sprint 2 §5).</summary>
    [HttpPost("{id:guid}/unlock")]
    [Authorize(Policy = "RequireAdministrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnlockUserCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Administrator-only: searches the audit trail (Sprint 2 §6), optionally scoped to one user.</summary>
    [HttpGet("audit-logs")]
    [Authorize(Policy = "RequireAdministrator")]
    [ProducesResponseType(typeof(PagedAuditLogListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAuditLogListDto>> GetAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserAuditLogsQuery(userId, action, fromUtc, toUtc, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

/// <summary>Request body for PUT /api/v1/users/me.</summary>
public record UpdateProfileRequest(string FullName, string? PhoneNumber);
