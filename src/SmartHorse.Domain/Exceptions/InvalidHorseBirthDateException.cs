namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a horse's birth date is in the future. Mapped to HTTP 400.</summary>
public class InvalidHorseBirthDateException : DomainException
{
    public InvalidHorseBirthDateException()
        : base("Birth date cannot be in the future.")
    {
    }
}
