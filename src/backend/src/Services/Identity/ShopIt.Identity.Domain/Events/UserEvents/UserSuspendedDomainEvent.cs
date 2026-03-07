using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserSuspendedDomainEvent(Guid userId, DateTime suspendedUntil, string reason) : IDomainEvent
{
    public Guid UserId { get; private set; } = userId;
    public DateTime SuspendedUntil { get; private set; } = suspendedUntil;
    public string Reason { get; private set; } = reason;
}
