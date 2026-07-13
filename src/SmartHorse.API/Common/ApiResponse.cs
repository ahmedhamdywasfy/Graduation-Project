namespace SmartHorse.API.Common;

/// <summary>
/// Consistent success-response envelope (Sprint 2 §8 — API Response Wrapper).
/// Applied automatically to every 2xx JSON response by <see cref="ApiResponseWrapperFilter"/>;
/// error responses intentionally keep the RFC 7807 ProblemDetails shape from
/// <see cref="Middleware.ExceptionHandlingMiddleware"/> (v0.1 §24) rather than
/// being force-fit into this envelope, since ProblemDetails is itself already a
/// consistent, standard error shape.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public T? Data { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T? data) => new() { Data = data };
}
