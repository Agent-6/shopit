using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserDeactivatedDomainEvent(Guid userId, string reason) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string Reason { get; private set; } = reason;
}
