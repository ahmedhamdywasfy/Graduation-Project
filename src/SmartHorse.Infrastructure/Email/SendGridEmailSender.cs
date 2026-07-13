using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SmartHorse.Infrastructure.Email;

/// <summary>
/// SendGrid-backed transport (Sprint 2 §1 "Prepare future SendGrid support").
/// Fully implemented against the real SendGrid API — activating it in production
/// is a one-line configuration change (<c>Email:Provider = "SendGrid"</c> plus an
/// API key), not a future development task.
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridSettings _sendGridSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(
        IOptions<SendGridSettings> sendGridSettings,
        IOptions<EmailSettings> emailSettings,
        ILogger<SendGridEmailSender> logger)
    {
        _sendGridSettings = sendGridSettings.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_sendGridSettings.ApiKey))
        {
            _logger.LogWarning(
                "Email:SendGrid:ApiKey is not configured — skipping send. Would have sent \"{Subject}\" to {ToEmail}.",
                subject, toEmail);
            return;
        }

        var client = new SendGridClient(_sendGridSettings.ApiKey);
        var from = new EmailAddress(_emailSettings.FromAddress, _emailSettings.FromName);
        var to = new EmailAddress(toEmail);
        var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(message, cancellationToken);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "SendGrid returned {StatusCode} sending \"{Subject}\" to {ToEmail}: {Body}",
                response.StatusCode, subject, toEmail, body);
            return;
        }

        _logger.LogInformation("Sent email \"{Subject}\" to {ToEmail} via SendGrid.", subject, toEmail);
    }
}
