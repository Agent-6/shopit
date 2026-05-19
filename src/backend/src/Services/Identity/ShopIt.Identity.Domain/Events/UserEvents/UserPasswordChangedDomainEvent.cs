using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserPasswordChangedDomainEvent(Guid userId, string securityStamp) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string SecurityStamp { get; private set; } = securityStamp;
}
