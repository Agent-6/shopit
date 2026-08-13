using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Application.Notifications;
using ShopIt.Identity.Application.Users.Activation;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Enums;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="ResendInvitationRequestedIntegrationEvent"/> from the Authentication
/// service, issues a fresh activation token for the invited user and re-publishes the
/// invitation notification so the Notifications service delivers the email again.
/// </summary>
public class ResendInvitationRequestedIntegrationEventHandler(
    UserManager<User> userManager,
    IActivationTokenProvider tokenProvider,
    IOutboxWriter outboxWriter,
    ICurrentTenant currentTenant,
    IOptions<EmailNotificationOptions> options,
    ILogger<ResendInvitationRequestedIntegrationEventHandler> logger) : IIntegrationEventHandler<ResendInvitationRequestedIntegrationEvent>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IActivationTokenProvider _tokenProvider = tokenProvider;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly EmailNotificationOptions _options = options.Value;
    private readonly ILogger<ResendInvitationRequestedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(ResendInvitationRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Inbox handlers run in a background scope with no HTTP context; resolve the user
        // at host scope (bypasses the tenant query filter — emails are globally unique).
        using var tenantChange = _currentTenant.Change(new TenantInfo(Guid.Empty, "Host"));

        var user = await _userManager.FindByEmailAsync(integrationEvent.Email);

        // Don't reveal whether an account exists.
        if (user is null)
        {
            _logger.LogInformation("Invitation resend requested for unknown email {Email}.", integrationEvent.Email);
            return;
        }

        if (user.Status != UserStatus.PendingActivation)
        {
            _logger.LogInformation(
                "Invitation resend skipped for {Email}: user status is {Status}.",
                integrationEvent.Email, user.Status);
            return;
        }

        var activationToken = _tokenProvider.Issue(user.Id);

        _logger.LogInformation(
            "New invitation token generated for {Email} (expires at {ExpiresAt}).",
            user.Email, activationToken.ExpiresAt);

        await _outboxWriter.WriteAsync(
            EmailMessageFactory.Invitation(
                _options,
                user.Id,
                user.Email!,
                activationToken.Token,
                activationToken.ExpiresAt),
            cancellationToken);
    }
}
