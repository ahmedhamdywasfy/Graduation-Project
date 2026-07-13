namespace SmartHorse.Infrastructure.Email;

/// <summary>Bound from the "Email:Smtp" configuration section. Credentials come from secrets/env, never hardcoded.</summary>
public class SmtpSettings
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
