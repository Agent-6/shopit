using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="PasswordResetRequestedIntegrationEvent"/> from the Authentication
/// service, applies the password reset and publishes the outcome via
/// <see cref="PasswordResetCompletedIntegrationEvent"/>.
/// </summary>
public class PasswordResetRequestedIntegrationEventHandler(
    UserManager<User> userManager,
    IOutboxWriter outboxWriter,
    ILogger<PasswordResetRequestedIntegrationEventHandler> logger) : IIntegrationEventHandler<PasswordResetRequestedIntegrationEvent>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<PasswordResetRequestedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(PasswordResetRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(integrationEvent.Email);

        if (user is null)
        {
            _logger.LogWarning("Password reset attempted for unknown email {Email}.", integrationEvent.Email);

            await _outboxWriter.WriteAsync(
                new PasswordResetCompletedIntegrationEvent(
                    integrationEvent.RequestId,
                    Guid.Empty,
                    integrationEvent.Email,
                    Succeeded: false,
                    Error: "Invalid password reset attempt."),
                cancellationToken);

            return;
        }

        var result = await _userManager.ResetPasswordAsync(user, integrationEvent.Token, integrationEvent.NewPassword);

        _logger.LogInformation(
            "Password reset {Outcome} for {Email}.",
            result.Succeeded ? "succeeded" : "failed",
            integrationEvent.Email);

        await _outboxWriter.WriteAsync(
            new PasswordResetCompletedIntegrationEvent(
                integrationEvent.RequestId,
                user.Id,
                user.Email!,
                result.Succeeded,
                result.Succeeded ? null : string.Join("; ", result.Errors.Select(e => e.Description))),
            cancellationToken);
    }
}
