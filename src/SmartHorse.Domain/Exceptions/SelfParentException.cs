namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a horse is assigned as its own parent. Mapped to HTTP 400.</summary>
public class SelfParentException : DomainException
{
    public SelfParentException()
        : base("A horse cannot be its own parent.")
    {
    }
}
