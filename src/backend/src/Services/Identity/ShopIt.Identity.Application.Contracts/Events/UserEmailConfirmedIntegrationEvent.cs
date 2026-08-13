using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service with the outcome of an email-confirmation attempt.
/// Consumed by the Authentication service so the confirmation page can show the result.
/// </summary>
/// <param name="RequestId">Correlation id echoed from the originating request.</param>
/// <param name="UserId">The id of the user whose email was confirmed (empty when the user was not found).</param>
/// <param name="Email">The email address that was confirmed.</param>
/// <param name="Succeeded">Whether the email was confirmed successfully.</param>
/// <param name="Error">A human-readable error when the confirmation failed.</param>
public record UserEmailConfirmedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    string Email,
    bool Succeeded,
    string? Error) : IntegrationEvent;
