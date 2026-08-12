using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="UserEmailConfirmedIntegrationEvent"/> from the Identity service
/// and records the outcome so the confirmation-processing page can stop polling and render the result.
/// </summary>
public class UserEmailConfirmedIntegrationEventHandler(
    IFlowStatusStore flowStatusStore,
    ILogger<UserEmailConfirmedIntegrationEventHandler> logger) : IIntegrationEventHandler<UserEmailConfirmedIntegrationEvent>
{
    private readonly IFlowStatusStore _flowStatusStore = flowStatusStore;
    private readonly ILogger<UserEmailConfirmedIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(UserEmailConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
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

        return Task.CompletedTask;
    }
}
