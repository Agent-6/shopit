using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserProfileUpdatedDomainEvent(Guid userId, string firstName, string lastName) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public string FirstName { get; private set; } = firstName;
    public string LastName { get; private set; } = lastName;
}
