using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service after it generated a password reset token.
/// Consumed by the Authentication service, which delivers it via the (mock) email
/// outbox so the user can follow the reset link.
/// </summary>
/// <param name="RequestId">Correlation id echoed from the originating request.</param>
/// <param name="UserId">The id of the user requesting the reset.</param>
/// <param name="Email">The email address the reset link is delivered to.</param>
/// <param name="Token">The password reset token.</param>
public record PasswordResetTokenGeneratedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    string Email,
    string Token) : IntegrationEvent;
