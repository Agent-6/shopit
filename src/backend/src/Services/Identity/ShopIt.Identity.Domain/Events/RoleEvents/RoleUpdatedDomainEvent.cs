using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleUpdatedDomainEvent(Guid RoleId, string Name, string? Description) : DomainEvent;
