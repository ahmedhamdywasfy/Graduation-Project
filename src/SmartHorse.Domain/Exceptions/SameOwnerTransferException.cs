namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when attempting to transfer a horse to its current owner. Mapped to HTTP 409.</summary>
public class SameOwnerTransferException : DomainException
{
    public SameOwnerTransferException()
        : base("This horse is already owned by the specified owner.")
    {
    }
}
