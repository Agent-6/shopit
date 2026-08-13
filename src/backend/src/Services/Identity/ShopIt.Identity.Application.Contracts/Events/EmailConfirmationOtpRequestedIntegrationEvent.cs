using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Authentication service when a user requests an email-confirmation
/// verification code. Consumed by the Identity service, which generates and stores the
/// OTP and publishes a
/// <see cref="ShopIt.Notifications.Application.Contracts.Events.SendEmailIntegrationEvent"/>
/// so the Notifications service delivers the code.
/// </summary>
/// <param name="RequestId">Correlation id used to trace the flow end-to-end.</param>
/// <param name="Email">The email address to confirm.</param>
public record EmailConfirmationOtpRequestedIntegrationEvent(Guid RequestId, string Email) : IntegrationEvent;
