namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when Delete is called on an already soft-deleted ownership record. Mapped to HTTP 409.</summary>
public class OwnershipRecordAlreadyDeletedException : DomainException
{
    public OwnershipRecordAlreadyDeletedException(Guid recordId)
        : base($"Ownership record \"{recordId}\" has already been deleted.")
    {
    }
}
