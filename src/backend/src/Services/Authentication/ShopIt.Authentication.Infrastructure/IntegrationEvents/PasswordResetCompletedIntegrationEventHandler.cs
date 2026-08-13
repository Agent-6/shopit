using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="PasswordResetCompletedIntegrationEvent"/> from the Identity service
/// and records the outcome so the reset-processing page can stop polling and render the result.
/// </summary>
public class PasswordResetCompletedIntegrationEventHandler(
    IFlowStatusStore flowStatusStore,
    ILogger<PasswordResetCompletedIntegrationEventHandler> logger) : IIntegrationEventHandler<PasswordResetCompletedIntegrationEvent>
{
    private readonly IFlowStatusStore _flowStatusStore = flowStatusStore;
    private readonly ILogger<PasswordResetCompletedIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(PasswordResetCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _flowStatusStore.Set(
            integrationEvent.RequestId,
            new FlowStatus(
                integrationEvent.RequestId,
                integrationEvent.Succeeded ? FlowState.Succeeded : FlowState.Failed,
                integrationEvent.Succeeded ? "Your password has been reset. You can now sign in with your new password." : integrationEvent.Error));

        _logger.LogInformation(
            "Password reset {Outcome} for {Email} (RequestId: {RequestId}).",
            integrationEvent.Succeeded ? "succeeded" : "failed",
            integrationEvent.Email,
            integrationEvent.RequestId);

        return Task.CompletedTask;
    }
}
