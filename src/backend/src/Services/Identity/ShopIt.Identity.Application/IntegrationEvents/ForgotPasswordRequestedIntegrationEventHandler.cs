using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Application.Notifications;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="ForgotPasswordRequestedIntegrationEvent"/> from the Authentication
/// service, generates a password reset token and publishes a
/// <see cref="ShopIt.Notifications.Application.Contracts.Events.SendEmailIntegrationEvent"/>
/// so the Notifications service delivers the reset link to the user.
/// </summary>
public class ForgotPasswordRequestedIntegrationEventHandler(
    UserManager<User> userManager,
    IOutboxWriter outboxWriter,
    ICurrentTenant currentTenant,
    IOptions<EmailNotificationOptions> options,
    ILogger<ForgotPasswordRequestedIntegrationEventHandler> logger) : IIntegrationEventHandler<ForgotPasswordRequestedIntegrationEvent>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly EmailNotificationOptions _options = options.Value;
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
            "Password reset token generated for {Email}.",
            user.Email);

        await _outboxWriter.WriteAsync(
            EmailMessageFactory.PasswordReset(_options, user.Id, user.Email!, token),
            cancellationToken);
    }
}
