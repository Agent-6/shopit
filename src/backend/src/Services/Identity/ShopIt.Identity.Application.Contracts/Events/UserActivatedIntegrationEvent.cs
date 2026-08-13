using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service when a user account becomes active — either through
/// the invite activation flow or an admin re-enabling the account. Intended for audit,
/// analytics and notification services.
/// </summary>
/// <param name="RequestId">Correlation id (echoed from the originating request when event-driven).</param>
/// <param name="UserId">The id of the activated user.</param>
/// <param name="Email">The user's email address.</param>
public record UserActivatedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    string Email) : IntegrationEvent;
