namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Testable wrapper around the system clock (always UTC — v0.1/v0.2 convention).</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
