using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="PasswordResetTokenGeneratedIntegrationEvent"/> from the Identity
/// service and delivers the reset link through the (mock) email outbox so the user can
/// follow it during development.
/// </summary>
public class PasswordResetTokenGeneratedIntegrationEventHandler(
    IMockEmailService mockEmailService,
    ILogger<PasswordResetTokenGeneratedIntegrationEventHandler> logger) : IIntegrationEventHandler<PasswordResetTokenGeneratedIntegrationEvent>
{
    private readonly IMockEmailService _mockEmailService = mockEmailService;
    private readonly ILogger<PasswordResetTokenGeneratedIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(PasswordResetTokenGeneratedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var email = integrationEvent.Email;
        var token = integrationEvent.Token;

        var resetLink = $"/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        _mockEmailService.Deliver(new MockEmail(
            email,
            "Reset your ShopIt password",
            $"We received a request to reset your password. Follow this link to choose a new one: {resetLink}",
            DateTime.UtcNow));

        _logger.LogInformation("Mock email delivered to {Email}: password reset link.", email);

        return Task.CompletedTask;
    }
}
