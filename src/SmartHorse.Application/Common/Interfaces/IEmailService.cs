namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Abstraction over outbound transactional email. Sprint 1 covered Forgot
/// Password only; Sprint 2 adds Email Confirmation. A full multi-template/
/// multi-channel notification system is designed in v0.2 Section 4 and remains
/// deferred to the Notifications module sprint — this interface intentionally
/// stays narrow (just the two transactional flows that exist so far) rather than
/// growing into that system prematurely.
///
/// Sprint 2's Infrastructure implementation renders an HTML template and sends
/// via a pluggable <c>IEmailSender</c> (SMTP now, SendGrid ready — see
/// Infrastructure/Email), selected by the "Email:Provider" configuration key.
/// </summary>
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetToken, CancellationToken cancellationToken = default);

    Task SendEmailConfirmationAsync(string toEmail, string recipientName, string confirmationToken, CancellationToken cancellationToken = default);
}
