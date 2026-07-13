namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Base type for all exceptions raised by domain entities when an invariant is
/// violated (e.g., deactivating an already-deactivated user). The API layer's
/// global exception handling middleware maps this to an appropriate HTTP status.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
