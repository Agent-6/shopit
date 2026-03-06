using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserDeactivatedDomainEvent(Guid userId, string reason) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string Reason { get; private set; } = reason;
}
