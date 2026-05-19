using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserExternalLoginRemovedDomainEvent(Guid userId, string loginProvider, string providerKey) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public string LoginProvider { get; } = loginProvider;
    public string ProviderKey { get; } = providerKey;
}
