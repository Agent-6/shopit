using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserActivatedDomainEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
}
