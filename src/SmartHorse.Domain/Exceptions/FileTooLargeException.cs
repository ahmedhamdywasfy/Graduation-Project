namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown by secure file upload validation when the file exceeds the configured size limit. Mapped to HTTP 400.</summary>
public class FileTooLargeException : DomainException
{
    public FileTooLargeException(long maxSizeBytes)
        : base($"File exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)} MB.")
    {
    }
}
