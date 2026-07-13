using Microsoft.AspNetCore.Http;
using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Services;

/// <summary>
/// Reads the caller's IP address and User-Agent from the current HttpContext
/// (Sprint 2 §6 — Audit Logs; §4 — refresh token session metadata). Falls back to
/// "unknown" outside a request (e.g., background jobs), never throws.
/// </summary>
public class RequestContextAccessor : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "unknown";
            }

            // Respect a reverse-proxy-forwarded IP if present (common in production
            // behind Nginx/Azure App Service — v0.1 §38 Deployment Strategy), else
            // fall back to the direct connection IP.
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public string UserAgent
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        }
    }
}
