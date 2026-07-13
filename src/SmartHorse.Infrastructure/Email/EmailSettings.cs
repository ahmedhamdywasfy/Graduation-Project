namespace SmartHorse.Infrastructure.Email;

/// <summary>
/// Top-level email configuration (Sprint 2 §1). "Provider" selects which
/// <c>IEmailSender</c> implementation is registered at startup — "Smtp" (default,
/// works out of the box against any SMTP relay) or "SendGrid" (Sprint 2 §1 "Prepare
/// future SendGrid support" — fully implemented here, just needs an API key to
/// activate). FrontendBaseUrl is used to build confirmation/reset links in the
/// HTML templates.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";
    public string FromAddress { get; set; } = "no-reply@smarthorse.local";
    public string FromName { get; set; } = "Smart Horse Management System";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
