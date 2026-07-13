namespace SmartHorse.Application.Users.DTOs;

/// <summary>Public-facing user projection — never exposes PasswordHash or any token hash.</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public string? AvatarUrl { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
}
