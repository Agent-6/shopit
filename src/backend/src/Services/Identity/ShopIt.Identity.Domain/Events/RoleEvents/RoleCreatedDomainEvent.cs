using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public class RoleCreatedDomainEvent(Role role) : IDomainEvent
{
    public Role Role { get; private set; } = role;
}
