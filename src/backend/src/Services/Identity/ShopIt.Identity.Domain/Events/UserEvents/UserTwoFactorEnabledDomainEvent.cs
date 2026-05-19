using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserTwoFactorEnabledDomainEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
}
