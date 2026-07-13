namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown by the Application layer's FluentValidation pipeline behavior when
/// one or more validators fail. Mapped to HTTP 400 with a field-level error
/// payload by the global exception handling middleware.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException()
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
