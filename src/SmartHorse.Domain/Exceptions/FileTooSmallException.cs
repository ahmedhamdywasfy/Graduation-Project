namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown by secure file upload validation when the file is below the configured minimum size (likely empty/corrupt). Mapped to HTTP 400.</summary>
public class FileTooSmallException : DomainException
{
    public FileTooSmallException(long minSizeBytes)
        : base($"File is smaller than the minimum allowed size of {minSizeBytes / 1024} KB.")
    {
    }
}
