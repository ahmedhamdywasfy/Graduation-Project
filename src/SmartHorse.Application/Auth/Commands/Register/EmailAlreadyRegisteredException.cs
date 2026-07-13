using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.Register;

/// <summary>Thrown on registration when the email is already in use. Mapped to HTTP 409.</summary>
public class EmailAlreadyRegisteredException : DomainException
{
    public EmailAlreadyRegisteredException(string email)
        : base($"An account with email \"{email}\" already exists.")
    {
    }
}
