namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when the assigned father/mother does not have the expected gender. Mapped to HTTP 400.</summary>
public class InvalidParentGenderException : DomainException
{
    public InvalidParentGenderException(string role, string expectedGender)
        : base($"The assigned {role} must have gender \"{expectedGender}\".")
    {
    }
}
