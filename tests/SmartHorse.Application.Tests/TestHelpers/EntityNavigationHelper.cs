using System.Reflection;

namespace SmartHorse.Application.Tests.TestHelpers;

/// <summary>
/// Test-only helper for setting private-setter navigation properties on domain
/// entities that would normally only be populated by EF Core's query
/// materialization. Several tests across Horses/Auth construct entities purely
/// in memory (no real DbContext), so this fills the same role EF Core's
/// relationship fixup plays in production.
/// </summary>
public static class EntityNavigationHelper
{
    public static void SetNavigation(object entity, string propertyName, object? value)
    {
        var property = entity.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{entity.GetType().Name} has no public property named {propertyName}.");

        property.SetValue(entity, value);
    }
}
