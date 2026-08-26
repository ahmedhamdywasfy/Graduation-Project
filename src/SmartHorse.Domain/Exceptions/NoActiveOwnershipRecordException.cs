namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a horse has no active (open) ownership record to close during a transfer — a data-integrity guard that should never trigger in normal operation. Mapped to HTTP 409.</summary>
public class NoActiveOwnershipRecordException : DomainException
{
    public NoActiveOwnershipRecordException(Guid horseId)
        : base($"Horse \"{horseId}\" has no active ownership record to transfer from.")
    {
    }
}
