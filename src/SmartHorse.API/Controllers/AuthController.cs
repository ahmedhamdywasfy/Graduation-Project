using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHorse.Application.Auth.Commands.ChangePassword;
using SmartHorse.Application.Auth.Commands.ConfirmEmail;
using SmartHorse.Application.Auth.Commands.ForgotPassword;
using SmartHorse.Application.Auth.Commands.Login;
using SmartHorse.Application.Auth.Commands.Logout;
using SmartHorse.Application.Auth.Commands.RefreshToken;
using SmartHorse.Application.Auth.Commands.Register;
using SmartHorse.Application.Auth.Commands.ResendConfirmationEmail;
using SmartHorse.Application.Auth.Commands.ResetPassword;
using SmartHorse.Application.Auth.DTOs;
using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.API.Controllers;

/// <summary>
/// Authentication endpoints (v0.1 Section 12 — Authentication Module; Sprint 2 §1
/// Email Confirmation and §2 Change Password). Thin controller: every action only
/// maps the HTTP request to a MediatR command and returns the result — no
/// business logic lives here (v0.1 Section 28). Versioned as v1.0 (Sprint 2 §13
/// — API Versioning) under /api/v1. The whole controller is subject to the
/// stricter "AuthPolicy" rate limit (v0.2 Security Review, Section 8) since it
/// is the highest-value target for credential-stuffing/brute-force.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth")]
[Produces("application/json")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentUserService _currentUser;

    public AuthController(ISender mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Registers a new account and immediately issues an access/refresh token pair.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Authenticates with email/password and issues an access/refresh token pair.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Exchanges a valid refresh token for a new access/refresh token pair (rotation).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Revokes the given refresh token, ending that session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Requests a password reset email. Always returns 204 regardless of whether the email exists.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Completes a password reset using the token issued by /forgot-password.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Confirms an account's email using the token issued at registration or by /resend-confirmation.</summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Re-issues and re-sends a confirmation email. Always returns 204 regardless of account state.</summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Changes the authenticated caller's password and revokes all other active sessions.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmNewPassword);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for POST /api/v1/auth/change-password — UserId is deliberately not a field (taken from the JWT).</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
