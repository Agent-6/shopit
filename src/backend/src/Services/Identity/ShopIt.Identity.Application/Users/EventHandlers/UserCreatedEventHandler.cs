using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Events.UserEvents;

namespace ShopIt.Identity.Application.Users.EventHandlers;

public class UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger) : IDomainEventHandler<UserCreatedDomainEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger = logger;

    public Task HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId}, {Email}", domainEvent.User.Id, domainEvent.User.Email);
        return Task.CompletedTask;
    }
}
