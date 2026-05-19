using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserLockedOutDomainEvent(Guid userId, DateTimeOffset lockoutEnd) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public DateTimeOffset LockoutEnd { get; private set; } = lockoutEnd;
}
