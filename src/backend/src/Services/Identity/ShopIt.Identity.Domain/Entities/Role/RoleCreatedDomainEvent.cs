using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.Role;

public class RoleCreatedDomainEvent(Role role) : IDomainEvent
{
    public Role Role { get; private set; } = role;
}
