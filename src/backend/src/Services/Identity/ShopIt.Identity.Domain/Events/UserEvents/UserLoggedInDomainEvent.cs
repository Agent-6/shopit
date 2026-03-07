using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserLoggedInDomainEvent(Guid userId, string loginProvider) : IDomainEvent
{
    public Guid UserId { get; private set;  } = userId;
    public string LoginProvider { get; private set;  } = loginProvider;
}
