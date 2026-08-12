using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="EmailConfirmationSubmittedIntegrationEvent"/> from the Authentication
/// service, validates the verification code, confirms the user's email and publishes the
/// outcome via <see cref="UserEmailConfirmedIntegrationEvent"/>.
/// </summary>
public class EmailConfirmationSubmittedIntegrationEventHandler(
    UserManager<User> userManager,
    IOutboxWriter outboxWriter,
    ILogger<EmailConfirmationSubmittedIntegrationEventHandler> logger) : IIntegrationEventHandler<EmailConfirmationSubmittedIntegrationEvent>
{
    private const string EmailConfirmationOtpProvider = "EmailConfirmation";
    private const string EmailConfirmationOtpName = "Otp";

    private readonly UserManager<User> _userManager = userManager;
    private readonly IOutboxWriter _outboxWriter = outboxWriter;
    private readonly ILogger<EmailConfirmationSubmittedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(EmailConfirmationSubmittedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(integrationEvent.Email);

        if (user is null)
        {
            _logger.LogWarning("Email confirmation attempted for unknown email {Email}.", integrationEvent.Email);

            await PublishOutcomeAsync(integrationEvent, Guid.Empty, succeeded: false, error: "Invalid email confirmation attempt.", cancellationToken);
            return;
        }

        if (user.EmailConfirmed)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);
            await PublishOutcomeAsync(integrationEvent, user.Id, succeeded: true, error: null, cancellationToken);
            return;
        }

        var stored = await _userManager.GetAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);
        if (string.IsNullOrEmpty(stored))
        {
            await PublishOutcomeAsync(integrationEvent, user.Id, succeeded: false, error: "No verification code has been issued. Request a new code.", cancellationToken);
            return;
        }

        var separatorIndex = stored.IndexOf('|');
        if (separatorIndex < 0
            || !DateTimeOffset.TryParse(stored[(separatorIndex + 1)..], out var expiresAt)
            || expiresAt < DateTimeOffset.UtcNow
            || !CodesMatch(integrationEvent.Code, stored[..separatorIndex]))
        {
            await PublishOutcomeAsync(integrationEvent, user.Id, succeeded: false, error: "The verification code is invalid or has expired.", cancellationToken);
            return;
        }

        // NOTE: ConfirmEmail() raises UserEmailConfirmedDomainEvent, but this inbox path
        // persists via the InboxProcessor's SaveChangesAsync (no UnitOfWork/dispatcher), so
        // domain events are not dispatched here. The integration event below is the contract
        // other services observe; if a domain-event handler is added later it must be wired
        // explicitly in this handler.
        user.ConfirmEmail();

        // Rotate the security stamp so tokens issued before confirmation are invalidated,
        // then discard the OTP. Both operations persist the user changes.
        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        var removeResult = await _userManager.RemoveAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);

        if (!stampResult.Succeeded || !removeResult.Succeeded)
        {
            await PublishOutcomeAsync(integrationEvent, user.Id, succeeded: false, error: "Failed to confirm the email address.", cancellationToken);
            return;
        }

        _logger.LogInformation("Email {Email} confirmed for user {UserId}.", user.Email, user.Id);

        await PublishOutcomeAsync(integrationEvent, user.Id, succeeded: true, error: null, cancellationToken);
    }

    private async Task PublishOutcomeAsync(
        EmailConfirmationSubmittedIntegrationEvent integrationEvent,
        Guid userId,
        bool succeeded,
        string? error,
        CancellationToken cancellationToken)
    {
        await _outboxWriter.WriteAsync(
            new UserEmailConfirmedIntegrationEvent(
                integrationEvent.RequestId,
                userId,
                integrationEvent.Email,
                succeeded,
                error),
            cancellationToken);
    }

    private static bool CodesMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
