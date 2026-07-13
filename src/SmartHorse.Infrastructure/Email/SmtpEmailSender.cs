using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartHorse.Infrastructure.Email;

/// <summary>Default email transport: any standard SMTP relay (Sprint 2 §1 — SMTP Provider).</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpSettings> smtpSettings,
        IOptions<EmailSettings> emailSettings,
        ILogger<SmtpEmailSender> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
        {
            // No SMTP host configured (common in local development without a real
            // relay set up yet) — log instead of throwing, so the surrounding
            // business flow (registration, password reset) still completes.
            _logger.LogWarning(
                "Email:Smtp:Host is not configured — skipping send. Would have sent \"{Subject}\" to {ToEmail}.",
                subject, toEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromAddress, _emailSettings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_smtpSettings.Username)
                ? null
                : new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent email \"{Subject}\" to {ToEmail} via SMTP.", subject, toEmail);
    }
}
