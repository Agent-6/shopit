using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleCreatedDomainEvent(Role role) : DomainEvent;
