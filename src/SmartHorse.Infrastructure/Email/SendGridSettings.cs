namespace SmartHorse.Infrastructure.Email;

/// <summary>Bound from the "Email:SendGrid" configuration section. ApiKey comes from secrets/env, never hardcoded.</summary>
public class SendGridSettings
{
    public const string SectionName = "Email:SendGrid";

    public string ApiKey { get; set; } = string.Empty;
}
