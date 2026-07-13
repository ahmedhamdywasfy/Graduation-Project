namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist. Mapped to HTTP 404 by the
/// global exception handling middleware in the API layer.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" with key ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
