using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service after an admin invites a user. Consumed by the
/// notification side (currently the Authentication service's mock email), which builds
/// the activation link and delivers the invitation email.
/// </summary>
/// <param name="RequestId">Correlation id (echoed from the originating request when event-driven).</param>
/// <param name="UserId">The id of the invited user.</param>
/// <param name="TenantId">The tenant the user belongs to.</param>
/// <param name="Email">The email address the invitation is delivered to.</param>
/// <param name="FirstName">The user's first name (may be empty).</param>
/// <param name="LastName">The user's last name (may be empty).</param>
/// <param name="ActivationToken">Time-limited, cryptographically signed activation token.</param>
/// <param name="ExpiresAt">UTC expiry of <paramref name="ActivationToken"/>.</param>
public record UserInvitedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string ActivationToken,
    DateTimeOffset ExpiresAt) : IntegrationEvent;
