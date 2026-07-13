namespace SmartHorse.Infrastructure.Email;

/// <summary>
/// Low-level "send this HTML email" transport abstraction. Deliberately separate
/// from the Application layer's <c>IEmailService</c>: <c>IEmailService</c> knows
/// about business flows (password reset, email confirmation) and renders
/// templates; <c>IEmailSender</c> only knows how to hand a rendered message to a
/// provider (SMTP or SendGrid). This split is what lets Sprint 2 "prepare future
/// SendGrid support" — swapping providers never touches template/business logic.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
