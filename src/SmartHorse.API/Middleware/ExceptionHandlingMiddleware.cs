using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.Auth.Commands.Register;
using SmartHorse.Application.Auth.Commands.ResetPassword;
using SmartHorse.Domain.Exceptions;
using ValidationException = SmartHorse.Domain.Exceptions.ValidationException;

namespace SmartHorse.API.Middleware;

/// <summary>
/// Global exception handling middleware (v0.1 Section 24). Converts every domain
/// exception into an RFC 7807 ProblemDetails response with a consistent shape and
/// the correct HTTP status code, and ensures unhandled exceptions never leak
/// internal details to the client — only to the logs (v0.1 Section 23).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{ExceptionType} handled for {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = statusCode == HttpStatusCode.InternalServerError && !_environment.IsDevelopment()
                ? "An unexpected error occurred. Please try again later."
                : exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        if (exception is AccountLockedException lockedException)
        {
            problemDetails.Extensions["lockedUntilUtc"] = lockedException.LockedUntilUtc;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
        ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
        InvalidHorseBirthDateException => (HttpStatusCode.BadRequest, "Invalid birth date"),
        InvalidHorseMeasurementException => (HttpStatusCode.BadRequest, "Invalid measurement"),
        InvalidPasswordResetTokenException => (HttpStatusCode.BadRequest, "Invalid reset token"),
        InvalidEmailConfirmationTokenException => (HttpStatusCode.BadRequest, "Invalid confirmation token"),
        InvalidCurrentPasswordException => (HttpStatusCode.BadRequest, "Incorrect current password"),
        UnsupportedFileTypeException => (HttpStatusCode.BadRequest, "Unsupported file type"),
        FileTooLargeException => (HttpStatusCode.BadRequest, "File too large"),
        FileTooSmallException => (HttpStatusCode.BadRequest, "File too small"),
        InvalidImageDimensionsException => (HttpStatusCode.BadRequest, "Invalid image dimensions"),
        SelfParentException => (HttpStatusCode.BadRequest, "Invalid parent assignment"),
        InvalidParentGenderException => (HttpStatusCode.BadRequest, "Invalid parent gender"),
        CircularLineageException => (HttpStatusCode.Conflict, "Circular lineage"),
        DuplicateHorseImageException => (HttpStatusCode.Conflict, "Duplicate image"),
        MaxImagesExceededException => (HttpStatusCode.Conflict, "Maximum images exceeded"),
        OwnershipRecordAlreadyDeletedException => (HttpStatusCode.Conflict, "Ownership record already deleted"),
        NoActiveOwnershipRecordException => (HttpStatusCode.Conflict, "No active ownership record"),
        SameOwnerTransferException => (HttpStatusCode.Conflict, "Already owned by this owner"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "Invalid credentials"),
        InvalidRefreshTokenException => (HttpStatusCode.Unauthorized, "Invalid refresh token"),
        AccountInactiveException => (HttpStatusCode.Forbidden, "Account inactive"),
        AccountLockedException => (HttpStatusCode.Locked, "Account locked"),
        EmailAlreadyRegisteredException => (HttpStatusCode.Conflict, "Email already registered"),
        EmailAlreadyConfirmedException => (HttpStatusCode.Conflict, "Email already confirmed"),
        DomainException => (HttpStatusCode.Conflict, "Request could not be completed"),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
    };
}
