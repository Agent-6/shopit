using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserAddedToRoleDomainEvent(Guid UserId, Guid RoleId, string? RoleName) : DomainEvent;
