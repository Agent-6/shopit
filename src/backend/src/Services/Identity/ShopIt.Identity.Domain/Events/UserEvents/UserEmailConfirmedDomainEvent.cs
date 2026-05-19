using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserEmailConfirmedDomainEvent(Guid userId, string Email) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string Email { get; private set; } = Email;
}
