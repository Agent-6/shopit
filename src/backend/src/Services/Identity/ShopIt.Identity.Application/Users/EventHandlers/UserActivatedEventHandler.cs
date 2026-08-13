using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Application.Notifications;
using ShopIt.Identity.Domain.Events.UserEvents;

namespace ShopIt.Identity.Application.Users.EventHandlers;

/// <summary>
/// Publishes <see cref="UserActivatedIntegrationEvent"/> when a user account becomes
/// active (invite activation or admin re-enable) for audit / analytics consumers, and a
/// <see cref="SendEmailIntegrationEvent"/> so the Notifications service can welcome the user.
/// </summary>
public class UserActivatedEventHandler(
    IOutboxWriter outboxWriter,
    ILogger<UserActivatedEventHandler> logger) : IDomainEventHandler<UserActivatedDomainEvent>
{
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<UserActivatedEventHandler> _logger = logger;

    public async Task HandleAsync(UserActivatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} ({Email}) activated.", domainEvent.UserId, domainEvent.Email);

        await _outboxWriter.WriteAsync(
            new UserActivatedIntegrationEvent(Guid.NewGuid(), domainEvent.UserId, domainEvent.Email),
            cancellationToken);

        await _outboxWriter.WriteAsync(
            EmailMessageFactory.AccountActivated(domainEvent.UserId, domainEvent.Email),
            cancellationToken);
    }
}
