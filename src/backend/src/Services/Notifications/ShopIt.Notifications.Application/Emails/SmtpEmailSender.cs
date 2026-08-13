using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ShopIt.Notifications.Application.Emails;

/// <summary>
/// Sends emails over SMTP. In development the endpoint is Mailpit's SMTP server
/// (no TLS, no authentication), so captured messages can be inspected in its web UI.
/// </summary>
public class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false,
        };
        mail.To.Add(message.To);

        using var client = new SmtpClient(_options.Host ?? "localhost", _options.Port ?? 1025)
        {
            EnableSsl = false, // Mailpit's dev SMTP server has no TLS
        };

        if (!string.IsNullOrEmpty(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(mail, cancellationToken);

        _logger.LogInformation(
            "Email sent to {To} via SMTP ({Host}:{Port}).",
            message.To, _options.Host ?? "localhost", _options.Port ?? 1025);
    }
}
