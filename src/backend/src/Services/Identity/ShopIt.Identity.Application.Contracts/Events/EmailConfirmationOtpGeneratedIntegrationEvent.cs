using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Identity service after it generated an email-confirmation OTP.
/// Consumed by the Authentication service, which delivers it via the (mock) email
/// outbox so the user can enter it in the confirmation form.
/// </summary>
/// <param name="RequestId">Correlation id echoed from the originating request.</param>
/// <param name="UserId">The id of the user requesting the code.</param>
/// <param name="Email">The email address the code is delivered to.</param>
/// <param name="Code">The 6-digit verification code.</param>
public record EmailConfirmationOtpGeneratedIntegrationEvent(
    Guid RequestId,
    Guid UserId,
    string Email,
    string Code) : IntegrationEvent;
