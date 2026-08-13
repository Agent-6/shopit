using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="EmailConfirmationOtpRequestedIntegrationEvent"/> from the
/// Authentication service, generates and stores a 6-digit verification code, then publishes
/// <see cref="EmailConfirmationOtpGeneratedIntegrationEvent"/> so the code can be delivered
/// (via email / mock email) to the user.
/// </summary>
public class EmailConfirmationOtpRequestedIntegrationEventHandler(
    UserManager<User> userManager,
    IOutboxWriter outboxWriter,
    ICurrentTenant currentTenant,
    ILogger<EmailConfirmationOtpRequestedIntegrationEventHandler> logger) : IIntegrationEventHandler<EmailConfirmationOtpRequestedIntegrationEvent>
{
    private const string EmailConfirmationOtpProvider = "EmailConfirmation";
    private const string EmailConfirmationOtpName = "Otp";
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<EmailConfirmationOtpRequestedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(EmailConfirmationOtpRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Inbox handlers run in a background scope with no HTTP context; resolve the user
        // at host scope (bypasses the tenant query filter — emails are globally unique).
        using var tenantChange = _currentTenant.Change(new TenantInfo(Guid.Empty, "Host"));

        var user = await _userManager.FindByEmailAsync(integrationEvent.Email);

        // Don't reveal whether an account exists.
        if (user is null)
        {
            _logger.LogInformation("Email confirmation requested for unknown email {Email}.", integrationEvent.Email);
            return;
        }

        if (user.EmailConfirmed)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);
        }

        var code = Random.Shared.Next(100000, 1000000).ToString("D6");
        var expiresAt = DateTime.UtcNow.Add(OtpLifetime);

        var result = await _userManager.SetAuthenticationTokenAsync(
            user,
            EmailConfirmationOtpProvider,
            EmailConfirmationOtpName,
            $"{code}|{expiresAt:O}");

        if (!result.Succeeded)
        {
            _logger.LogError("Failed to generate an email confirmation code for {Email}.", integrationEvent.Email);
            return;
        }

        _logger.LogInformation("Email confirmation code generated for {Email} (expires at {ExpiresAt}).", user.Email, expiresAt);

        await _outboxWriter.WriteAsync(
            new EmailConfirmationOtpGeneratedIntegrationEvent(
                integrationEvent.RequestId,
                user.Id,
                user.Email!,
                code),
            cancellationToken);
    }
}
