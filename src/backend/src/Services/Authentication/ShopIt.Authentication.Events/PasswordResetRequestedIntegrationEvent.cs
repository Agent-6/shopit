using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Authentication.Events;

/// <summary>
/// Published by the Authentication service when a user submits the password reset form.
/// Consumed by the Identity service, which applies the reset and replies with
/// <c>PasswordResetCompletedIntegrationEvent</c>.
/// </summary>
/// <param name="RequestId">Correlation id used to trace the flow end-to-end.</param>
/// <param name="Email">The email address of the account being reset.</param>
/// <param name="Token">The password reset token previously issued for this account.</param>
/// <param name="NewPassword">The new password chosen by the user.</param>
public record PasswordResetRequestedIntegrationEvent(
    Guid RequestId,
    string Email,
    string Token,
    string NewPassword) : IntegrationEvent;
