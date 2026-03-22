using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserAddedToRoleDomainEvent(Guid userId, Guid roleId, string? roleName) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public Guid RoleId { get; } = roleId;
    public string? RoleName { get; } = roleName;
}
