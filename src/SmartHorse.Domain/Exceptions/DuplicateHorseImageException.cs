namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when the exact same image (by content hash) is already attached to this horse. Mapped to HTTP 409.</summary>
public class DuplicateHorseImageException : DomainException
{
    public DuplicateHorseImageException()
        : base("This exact image has already been uploaded for this horse.")
    {
    }
}
