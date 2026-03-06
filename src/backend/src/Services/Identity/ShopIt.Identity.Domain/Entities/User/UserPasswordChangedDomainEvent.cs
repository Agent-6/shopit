using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserPasswordChangedDomainEvent(Guid userId, string securityStamp) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string SecurityStamp { get; private set; } = securityStamp;
}
