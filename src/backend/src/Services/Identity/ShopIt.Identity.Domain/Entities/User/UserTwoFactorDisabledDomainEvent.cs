using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserTwoFactorDisabledDomainEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
}
