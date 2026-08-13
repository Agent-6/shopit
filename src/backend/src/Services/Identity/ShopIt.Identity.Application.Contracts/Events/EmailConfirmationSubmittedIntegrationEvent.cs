using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Authentication service when a user submits a verification code.
/// Consumed by the Identity service, which validates the code, confirms the email and
/// replies with <see cref="UserEmailConfirmedIntegrationEvent"/>.
/// </summary>
/// <param name="RequestId">Correlation id used to trace the flow end-to-end.</param>
/// <param name="Email">The email address being confirmed.</param>
/// <param name="Code">The verification code supplied by the user.</param>
public record EmailConfirmationSubmittedIntegrationEvent(Guid RequestId, string Email, string Code) : IntegrationEvent;
