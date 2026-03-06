using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserEmailChangedDomainEvent(Guid userId, string newEmail) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string NewEmail { get; private set; } = newEmail;
}
