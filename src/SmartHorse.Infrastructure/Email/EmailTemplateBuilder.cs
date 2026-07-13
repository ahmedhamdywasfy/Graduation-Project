using System.Net;

namespace SmartHorse.Infrastructure.Email;

/// <summary>
/// Builds the HTML bodies for transactional emails (Sprint 2 §1 — HTML Email
/// Template). Kept intentionally simple (inline CSS, table-based layout) for
/// maximum email-client compatibility rather than pulling in a templating engine
/// for two templates; a Razor-based renderer is a reasonable upgrade once the
/// full Notifications module (v0.2 §4, NotificationTemplates table) arrives and
/// templates become admin-editable.
/// </summary>
public static class EmailTemplateBuilder
{
    public static string BuildEmailConfirmation(string recipientName, string confirmationLink)
    {
        return Wrap(
            title: "Confirm your email",
            bodyHtml: $"""
                <p>Hi {WebUtility.HtmlEncode(recipientName)},</p>
                <p>Thanks for registering with the Smart Horse Management System. Please confirm your email address to activate all account features.</p>
                <p style="text-align:center;margin:32px 0;">
                    <a href="{confirmationLink}" style="background:#2f6f4f;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">Confirm Email</a>
                </p>
                <p>This link expires in 24 hours. If you didn't create this account, you can safely ignore this email.</p>
                """);
    }

    public static string BuildPasswordReset(string recipientName, string resetLink)
    {
        return Wrap(
            title: "Reset your password",
            bodyHtml: $"""
                <p>Hi {WebUtility.HtmlEncode(recipientName)},</p>
                <p>We received a request to reset your Smart Horse Management System password.</p>
                <p style="text-align:center;margin:32px 0;">
                    <a href="{resetLink}" style="background:#2f6f4f;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">Reset Password</a>
                </p>
                <p>This link expires in 30 minutes. If you didn't request this, you can safely ignore this email — your password will not be changed.</p>
                """);
    }

    private static string Wrap(string title, string bodyHtml) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /><title>{WebUtility.HtmlEncode(title)}</title></head>
        <body style="font-family:Segoe UI,Arial,sans-serif;background:#f4f4f4;padding:24px;margin:0;">
            <table role="presentation" width="100%" style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:8px;overflow:hidden;">
                <tr><td style="background:#1f3d2b;padding:20px 24px;">
                    <span style="color:#ffffff;font-size:18px;font-weight:bold;">Smart Horse Management System</span>
                </td></tr>
                <tr><td style="padding:24px;color:#222222;font-size:14px;line-height:1.6;">
                    {bodyHtml}
                </td></tr>
                <tr><td style="padding:16px 24px;background:#f0f0f0;color:#888888;font-size:12px;">
                    This is an automated message — please do not reply directly to this email.
                </td></tr>
            </table>
        </body>
        </html>
        """;
}
