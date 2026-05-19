using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserCreatedDomainEvent(User user) : IDomainEvent
{
    public User User { get; private set; } = user;
}
