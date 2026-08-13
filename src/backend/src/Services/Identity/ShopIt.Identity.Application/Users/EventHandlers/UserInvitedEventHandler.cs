using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Domain.Events.UserEvents;

namespace ShopIt.Identity.Application.Users.EventHandlers;

/// <summary>
/// Publishes <see cref="UserInvitedIntegrationEvent"/> into the outbox when a user is
/// invited. Runs inside the command transaction (via the UnitOfWork domain-event
/// dispatch), so the outbox write is atomic with the user creation.
/// </summary>
public class UserInvitedEventHandler(
    IOutboxWriter outboxWriter,
    ILogger<UserInvitedEventHandler> logger) : IDomainEventHandler<UserInvitedDomainEvent>
{
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<UserInvitedEventHandler> _logger = logger;

    public async Task HandleAsync(UserInvitedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var user = domainEvent.User;

        _logger.LogInformation(
            "User {UserId} invited (expires at {ExpiresAt}).",
            user.Id, domainEvent.ActivationTokenExpiresAt);

        await _outboxWriter.WriteAsync(
            new UserInvitedIntegrationEvent(
                Guid.NewGuid(),
                user.Id,
                user.TenantId,
                user.Email!,
                user.FirstName ?? string.Empty,
                user.LastName ?? string.Empty,
                domainEvent.ActivationToken,
                domainEvent.ActivationTokenExpiresAt),
            cancellationToken);
    }
}
