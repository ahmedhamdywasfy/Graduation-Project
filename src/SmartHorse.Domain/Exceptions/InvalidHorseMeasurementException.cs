namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a horse's weight or height falls outside the allowed sanity range. Mapped to HTTP 400.</summary>
public class InvalidHorseMeasurementException : DomainException
{
    public InvalidHorseMeasurementException(string fieldName, decimal min, decimal max)
        : base($"{fieldName} must be between {min} and {max}.")
    {
    }
}
