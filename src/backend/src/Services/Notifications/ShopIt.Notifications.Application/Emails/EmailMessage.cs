namespace ShopIt.Notifications.Application.Emails;

/// <summary>
/// An email as it would be handed to a real SMTP provider.
/// </summary>
/// <param name="To">The recipient address.</param>
/// <param name="Subject">The email subject.</param>
/// <param name="Body">The email body.</param>
public record EmailMessage(string To, string Subject, string Body);

/// <summary>
/// Abstraction over the email transport. Development sends through Mailpit's SMTP
/// endpoint; a real provider can be swapped in without touching the event pipeline.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
