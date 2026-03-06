using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserLockedOutDomainEvent(Guid userId, DateTime lockoutEnd) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public DateTime LockoutEnd { get; private set; } = lockoutEnd;
}
