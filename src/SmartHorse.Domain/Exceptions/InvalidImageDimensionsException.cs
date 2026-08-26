namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when an uploaded image's dimensions fall outside the allowed range. Mapped to HTTP 400.</summary>
public class InvalidImageDimensionsException : DomainException
{
    private InvalidImageDimensionsException(string message) : base(message)
    {
    }

    public static InvalidImageDimensionsException TooSmall(int minWidth, int minHeight) =>
        new($"Image must be at least {minWidth}x{minHeight} pixels.");

    public static InvalidImageDimensionsException TooLarge(int maxWidth, int maxHeight) =>
        new($"Image must be no larger than {maxWidth}x{maxHeight} pixels.");
}
