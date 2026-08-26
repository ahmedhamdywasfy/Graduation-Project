namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when Delete is called on an already soft-deleted horse. Mapped to HTTP 409.</summary>
public class HorseAlreadyDeletedException : DomainException
{
    public HorseAlreadyDeletedException(Guid horseId)
        : base($"Horse \"{horseId}\" has already been deleted.")
    {
    }
}
