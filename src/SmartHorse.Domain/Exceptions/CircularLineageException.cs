namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when assigning a parent would create a circular ancestry (the horse would become its own ancestor). Mapped to HTTP 409.</summary>
public class CircularLineageException : DomainException
{
    public CircularLineageException()
        : base("This parent assignment would create a circular lineage (the horse would become its own ancestor).")
    {
    }
}
