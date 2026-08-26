namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a microchip number is already registered to another horse. Mapped to HTTP 409.</summary>
public class DuplicateMicrochipNumberException : DomainException
{
    public DuplicateMicrochipNumberException(string microchipNumber)
        : base($"Microchip number \"{microchipNumber}\" is already registered to another horse.")
    {
    }
}
