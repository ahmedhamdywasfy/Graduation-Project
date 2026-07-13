namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Exposes the identity of the currently authenticated caller to the Application
/// layer without leaking a dependency on HttpContext. Implemented in the API layer
/// (reads the validated JWT claims) and registered as scoped in DI.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);
}
