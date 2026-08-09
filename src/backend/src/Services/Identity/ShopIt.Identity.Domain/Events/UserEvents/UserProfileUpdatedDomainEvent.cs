using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserProfileUpdatedDomainEvent(Guid UserId, string FirstName, string LastName) : DomainEvent;
