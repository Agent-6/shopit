using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Application.Notifications;
using ShopIt.Identity.Domain.Events.UserEvents;

namespace ShopIt.Identity.Application.Users.EventHandlers;

/// <summary>
/// Publishes a <see cref="SendEmailIntegrationEvent"/> into the outbox when a user is
/// invited, so the Notifications service delivers the activation email. Runs inside the
/// command transaction (via the UnitOfWork domain-event dispatch), so the outbox write
/// is atomic with the user creation.
/// </summary>
public class UserInvitedEventHandler(
    IOutboxWriter outboxWriter,
    IOptions<EmailNotificationOptions> options,
    ILogger<UserInvitedEventHandler> logger) : IDomainEventHandler<UserInvitedDomainEvent>
{
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly EmailNotificationOptions _options = options.Value;
    private readonly ILogger<UserInvitedEventHandler> _logger = logger;

    public async Task HandleAsync(UserInvitedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var user = domainEvent.User;

        _logger.LogInformation(
            "User {UserId} invited (expires at {ExpiresAt}).",
            user.Id, domainEvent.ActivationTokenExpiresAt);

        await _outboxWriter.WriteAsync(
            EmailMessageFactory.Invitation(
                _options,
                user.Id,
                user.Email!,
                domainEvent.ActivationToken,
                domainEvent.ActivationTokenExpiresAt),
            cancellationToken);
    }
}
