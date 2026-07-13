using Serilog.Context;

namespace SmartHorse.API.Middleware;

/// <summary>
/// Attaches a correlation ID to every request (v0.1 Section 23 — Logging
/// Strategy), reusing an inbound "X-Correlation-Id" header if the client
/// supplied one, otherwise generating a new one. Pushed into the Serilog
/// LogContext so every log line for this request carries it automatically.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
