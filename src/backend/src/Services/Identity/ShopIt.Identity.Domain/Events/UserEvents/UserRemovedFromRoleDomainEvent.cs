using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserRemovedFromRoleDomainEvent(Guid UserId, Guid RoleId, string? RoleName) : DomainEvent;
