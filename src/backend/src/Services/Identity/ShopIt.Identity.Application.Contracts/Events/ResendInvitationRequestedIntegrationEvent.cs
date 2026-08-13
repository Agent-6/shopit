using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Authentication service when a user asks for a new invitation link
/// (from the login page or the expired-invitation page). Consumed by the Identity service,
/// which regenerates the activation token and publishes a
/// <see cref="ShopIt.Notifications.Application.Contracts.Events.SendEmailIntegrationEvent"/>
/// so the Notifications service delivers the invitation email again.
/// </summary>
/// <param name="RequestId">Correlation id used to trace the flow end-to-end.</param>
/// <param name="Email">The email address for which a new invitation was requested.</param>
public record ResendInvitationRequestedIntegrationEvent(Guid RequestId, string Email) : IntegrationEvent;
