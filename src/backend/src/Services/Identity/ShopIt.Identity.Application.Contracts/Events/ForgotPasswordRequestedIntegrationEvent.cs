using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Authentication service when a user requests a password reset.
/// Consumed by the Identity service, which generates the reset token and replies
/// with <see cref="PasswordResetTokenGeneratedIntegrationEvent"/>.
/// </summary>
/// <param name="RequestId">Correlation id used to trace the flow end-to-end.</param>
/// <param name="Email">The email address for which a reset was requested.</param>
public record ForgotPasswordRequestedIntegrationEvent(Guid RequestId, string Email) : IntegrationEvent;
