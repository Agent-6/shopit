using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserProfileUpdatedDomainEvent(Guid userId, string firstName, string lastName) : DomainEvent;
