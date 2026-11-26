using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@sustainmonitor.local";
    public string FromName { get; set; } = "Sustain Monitor";
}

public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Sends mail through SMTP when <c>Smtp:Host</c> is set; otherwise writes the message to the log,
/// so development works without a mail server.
/// </summary>
public sealed class EmailSender(IOptions<SmtpOptions> options, ILogger<EmailSender> log) : IAppEmailSender, IEmailSender<AppUser>
{
    private readonly SmtpOptions _o = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_o.Host))
        {
            log.LogInformation("Email not sent (Smtp:Host not set). To: {To} | {Subject}\n{Body}", to, subject, body);
            return;
        }

        using var client = new SmtpClient(_o.Host, _o.Port) { EnableSsl = _o.EnableSsl };
        if (!string.IsNullOrEmpty(_o.User))
            client.Credentials = new NetworkCredential(_o.User, _o.Password);

        using var message = new MailMessage
        {
            From = new MailAddress(_o.From, _o.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(to);
        await client.SendMailAsync(message, ct);
    }

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Sustain Monitor email", $"Hello {user.FullName},\n\nConfirm your email: {confirmationLink}");

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Sustain Monitor password", $"Hello {user.FullName},\n\nReset your password: {resetLink}");

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) =>
        SendAsync(email, "Your Sustain Monitor reset code",
            $"Hello {user.FullName},\n\nYour password reset code is:\n\n{resetCode}\n\nUse it with POST /api/auth/resetPassword. If you did not ask for this, ignore this email.");
}
