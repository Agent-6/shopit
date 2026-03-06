using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.User;

public class UserEmailConfirmedDomainEvent(Guid userId, string Email) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string Email { get; private set; } = Email;
}
