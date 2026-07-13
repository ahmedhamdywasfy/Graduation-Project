using Microsoft.Extensions.Options;
using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Email;

/// <summary>
/// Implements the Application layer's <see cref="IEmailService"/> by rendering
/// the appropriate HTML template (<see cref="EmailTemplateBuilder"/>) and handing
/// it to whichever <see cref="IEmailSender"/> was registered (SMTP or SendGrid —
/// see <see cref="DependencyInjection"/>). This is the only class that knows both
/// "which business flow" and "which template/link" — everything below it is
/// generic transport, everything above it (Application layer) is generic
/// "send a confirmation/reset email" intent.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;

    public EmailService(IEmailSender emailSender, IOptions<EmailSettings> emailSettings)
    {
        _emailSender = emailSender;
        _emailSettings = emailSettings.Value;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";
        var html = EmailTemplateBuilder.BuildPasswordReset(recipientName, link);
        return _emailSender.SendAsync(toEmail, "Reset your Smart Horse Management System password", html, cancellationToken);
    }

    public Task SendEmailConfirmationAsync(string toEmail, string recipientName, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/confirm-email?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(confirmationToken)}";
        var html = EmailTemplateBuilder.BuildEmailConfirmation(recipientName, link);
        return _emailSender.SendAsync(toEmail, "Confirm your Smart Horse Management System email", html, cancellationToken);
    }
}
