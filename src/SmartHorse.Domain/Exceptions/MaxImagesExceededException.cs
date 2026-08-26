namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a horse's image gallery is already at its maximum size. Mapped to HTTP 409.</summary>
public class MaxImagesExceededException : DomainException
{
    public MaxImagesExceededException(int maxImages)
        : base($"A horse cannot have more than {maxImages} images. Delete an existing image before uploading another.")
    {
    }
}
