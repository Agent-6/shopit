using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities.Role;

public class RoleUpdatedDomainEvent(Guid roleId, string name, string? description) : IDomainEvent
{
    public Guid RoleId { get; } = roleId;
    public string Name { get; } = name;
    public string? Description { get; } = description;
}
