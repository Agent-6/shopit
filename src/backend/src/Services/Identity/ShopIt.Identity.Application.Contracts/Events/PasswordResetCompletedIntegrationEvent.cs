using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service with the outcome of a password reset attempt.
/// Consumed by the Authentication service so the reset form can show the result.
/// </summary>
/// <param name="RequestId">Correlation id echoed from the originating request.</param>
/// <param name="UserId">The id of the user whose password was reset (empty when the user was not found).</param>
/// <param name="Email">The email address of the account.</param>
/// <param name="Succeeded">Whether the password was reset successfully.</param>
/// <param name="Error">A human-readable error when the reset failed.</param>
public record PasswordResetCompletedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    string Email,
    bool Succeeded,
    string? Error) : IntegrationEvent;
