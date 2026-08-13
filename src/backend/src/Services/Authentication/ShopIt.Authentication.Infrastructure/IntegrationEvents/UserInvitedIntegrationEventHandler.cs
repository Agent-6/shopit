using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="UserInvitedIntegrationEvent"/> from the Identity service and delivers
/// the activation email through the (mock) email outbox. The link points at this service's
/// MVC activation page, which collects the new password and calls Identity synchronously.
/// </summary>
public class UserInvitedIntegrationEventHandler(
    IMockEmailService mockEmailService,
    ILogger<UserInvitedIntegrationEventHandler> logger) : IIntegrationEventHandler<UserInvitedIntegrationEvent>
{
    private const string DefaultClientId = "angular-spa";

    private readonly IMockEmailService _mockEmailService = mockEmailService;
    private readonly ILogger<UserInvitedIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(UserInvitedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var email = integrationEvent.Email;

        var activationLink = $"/Account/Activate?userId={integrationEvent.UserId}" +
                             $"&token={Uri.EscapeDataString(integrationEvent.ActivationToken)}" +
                             $"&clientId={Uri.EscapeDataString(DefaultClientId)}";

        _mockEmailService.Deliver(new MockEmail(
            email,
            "Activate your ShopIt account",
            $"You've been invited to join ShopIt. Click this link to set your password and activate your account: {activationLink}" +
            $"\n\nThe invitation link expires on {integrationEvent.ExpiresAt:g} (UTC).",
            DateTime.UtcNow));

        _logger.LogInformation(
            "Mock invitation email delivered to {Email} (expires {ExpiresAt}).",
            email, integrationEvent.ExpiresAt);

        return Task.CompletedTask;
    }
}
