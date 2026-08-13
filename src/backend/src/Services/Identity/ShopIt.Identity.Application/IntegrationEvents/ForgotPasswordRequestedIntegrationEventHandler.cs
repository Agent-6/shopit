using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="ForgotPasswordRequestedIntegrationEvent"/> from the Authentication
/// service, generates a password reset token and publishes
/// <see cref="PasswordResetTokenGeneratedIntegrationEvent"/> so the token can be delivered
/// (via email / mock email) to the user.
/// </summary>
public class ForgotPasswordRequestedIntegrationEventHandler(
    UserManager<User> userManager,
    IOutboxWriter outboxWriter,
    ICurrentTenant currentTenant,
    ILogger<ForgotPasswordRequestedIntegrationEventHandler> logger) : IIntegrationEventHandler<ForgotPasswordRequestedIntegrationEvent>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<ForgotPasswordRequestedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(ForgotPasswordRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Inbox handlers run in a background scope with no HTTP context; resolve the user
        // at host scope (bypasses the tenant query filter — emails are globally unique).
        using var tenantChange = _currentTenant.Change(new TenantInfo(Guid.Empty, "Host"));

        var user = await _userManager.FindByEmailAsync(integrationEvent.Email);

        // Don't reveal whether an account exists.
        if (user is null)
        {
            _logger.LogInformation("Password reset requested for unknown email {Email}.", integrationEvent.Email);
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation(
            "Password reset token generated for {Email}. Mock delivery: /Account/ResetPassword?email={Email}&token={Token}",
            user.Email, user.Email, token);

        await _outboxWriter.WriteAsync(
            new PasswordResetTokenGeneratedIntegrationEvent(
                integrationEvent.RequestId,
                user.Id,
                user.Email!,
                token),
            cancellationToken);
    }
}
