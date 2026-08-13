using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Notifications.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="UserEmailConfirmedIntegrationEvent"/> from the Identity service,
/// records the outcome so the confirmation-processing page can stop polling and render the
/// result, and publishes a <see cref="SendEmailIntegrationEvent"/> so the Notifications
/// service can confirm the change by email.
/// </summary>
public class UserEmailConfirmedIntegrationEventHandler(
    IFlowStatusStore flowStatusStore,
    IOutboxWriter outboxWriter,
    ILogger<UserEmailConfirmedIntegrationEventHandler> logger) : IIntegrationEventHandler<UserEmailConfirmedIntegrationEvent>
{
    private readonly IFlowStatusStore _flowStatusStore = flowStatusStore;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<UserEmailConfirmedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(UserEmailConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _flowStatusStore.Set(
            integrationEvent.RequestId,
            new FlowStatus(
                integrationEvent.RequestId,
                integrationEvent.Succeeded ? FlowState.Succeeded : FlowState.Failed,
                integrationEvent.Succeeded
                    ? "Your email address has been confirmed. You can now sign in."
                    : integrationEvent.Error ?? "The verification code is invalid or has expired. Please try again."));

        _logger.LogInformation(
            "Email confirmation {Outcome} for {Email} (RequestId: {RequestId}).",
            integrationEvent.Succeeded ? "succeeded" : "failed",
            integrationEvent.Email,
            integrationEvent.RequestId);

        if (integrationEvent.Succeeded)
        {
            await _outboxWriter.WriteAsync(
                new SendEmailIntegrationEvent(
                    integrationEvent.UserId,
                    integrationEvent.Email,
                    "Your ShopIt email address has been confirmed",
                    "Your ShopIt email address has been confirmed. You can now sign in."),
                cancellationToken);
        }
    }
}
