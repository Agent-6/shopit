using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Notifications.Application.Contracts.Events;
using ShopIt.Notifications.Application.Emails;

namespace ShopIt.Notifications.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="SendEmailIntegrationEvent"/> published by the Identity and
/// Authentication services and delivers the email through the configured
/// <see cref="IEmailSender"/>.
/// </summary>
public class SendEmailIntegrationEventHandler(
    IEmailSender emailSender,
    ILogger<SendEmailIntegrationEventHandler> logger) : IIntegrationEventHandler<SendEmailIntegrationEvent>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<SendEmailIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(SendEmailIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling email notification for user {UserId} ({Email}).",
            integrationEvent.UserId, integrationEvent.Email);

        return _emailSender.SendAsync(
            new EmailMessage(
                integrationEvent.Email,
                integrationEvent.Subject,
                integrationEvent.Message),
            cancellationToken);
    }
}
