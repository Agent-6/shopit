using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserCreatedDomainEvent(User User) : DomainEvent;
