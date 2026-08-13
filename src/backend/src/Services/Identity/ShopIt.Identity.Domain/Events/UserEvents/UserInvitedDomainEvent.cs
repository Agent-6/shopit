using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.UserEvents;

/// <summary>
/// Raised when a user is provisioned via the invite flow (Status = PendingActivation).
/// Carries the time-limited activation token so application handlers can publish the
/// <c>UserInvitedIntegrationEvent</c> that ultimately delivers the email.
/// </summary>
public record UserInvitedDomainEvent(
    User User,
    string ActivationToken,
    DateTimeOffset ActivationTokenExpiresAt) : DomainEvent;
