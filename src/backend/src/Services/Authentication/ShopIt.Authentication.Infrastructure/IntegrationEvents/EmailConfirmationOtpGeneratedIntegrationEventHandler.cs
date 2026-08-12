using Microsoft.Extensions.Logging;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;

namespace ShopIt.Authentication.Infrastructure.IntegrationEvents;

/// <summary>
/// Consumes <see cref="EmailConfirmationOtpGeneratedIntegrationEvent"/> from the Identity
/// service and delivers the verification code through the (mock) email outbox so the user
/// can read it during development.
/// </summary>
public class EmailConfirmationOtpGeneratedIntegrationEventHandler(
    IMockEmailService mockEmailService,
    ILogger<EmailConfirmationOtpGeneratedIntegrationEventHandler> logger) : IIntegrationEventHandler<EmailConfirmationOtpGeneratedIntegrationEvent>
{
    private readonly IMockEmailService _mockEmailService = mockEmailService;
    private readonly ILogger<EmailConfirmationOtpGeneratedIntegrationEventHandler> _logger = logger;

    public Task HandleAsync(EmailConfirmationOtpGeneratedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _mockEmailService.Deliver(new MockEmail(
            integrationEvent.Email,
            "Your ShopIt verification code",
            $"Your email confirmation code is {integrationEvent.Code}. It expires in 10 minutes.",
            DateTime.UtcNow));

        _logger.LogInformation("Mock email delivered to {Email}: confirmation code.", integrationEvent.Email);

        return Task.CompletedTask;
    }
}
