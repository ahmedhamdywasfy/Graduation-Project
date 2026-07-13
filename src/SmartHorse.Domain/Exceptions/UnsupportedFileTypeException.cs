namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown by secure file upload validation when the content type is not allow-listed. Mapped to HTTP 400.</summary>
public class UnsupportedFileTypeException : DomainException
{
    public UnsupportedFileTypeException(string contentType)
        : base($"File type \"{contentType}\" is not supported. Allowed types: image/jpeg, image/png, image/webp.")
    {
    }
}
