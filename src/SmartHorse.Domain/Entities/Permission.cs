using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A single fine-grained permission (e.g., "reports.export"), as introduced in
/// v0.2 Section 2.2 (Administration Module). Sprint 1 only establishes the schema
/// and seed mechanism for permissions used by Identity/User Management; module-
/// specific permissions (Horse, Medical, Marketplace, ...) are added by later
/// sprints when those modules are implemented.
/// </summary>
public class Permission : BaseIntEntity
{
    private Permission()
    {
        Key = string.Empty;
        Description = string.Empty;
    }

    public Permission(string key, string description)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Permission key cannot be empty.", nameof(key));
        }

        Key = key.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    /// <summary>Dotted, machine-readable identifier, e.g. "users.manage".</summary>
    public string Key { get; private set; }

    public string Description { get; private set; }
}
