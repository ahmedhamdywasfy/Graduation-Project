namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when Restore is called on a horse that is not currently deleted. Mapped to HTTP 409.</summary>
public class HorseNotDeletedException : DomainException
{
    public HorseNotDeletedException(Guid horseId)
        : base($"Horse \"{horseId}\" is not deleted, so it cannot be restored.")
    {
    }
}
