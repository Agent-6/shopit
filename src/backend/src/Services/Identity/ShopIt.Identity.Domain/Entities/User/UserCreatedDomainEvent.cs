using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserCreatedDomainEvent(User user) : IDomainEvent
{
    public User User { get; private set; } = user;
}
