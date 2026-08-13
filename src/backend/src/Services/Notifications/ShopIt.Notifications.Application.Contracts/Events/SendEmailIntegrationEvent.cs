using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Notifications.Application.Contracts.Events;

/// <summary>
/// Published by the Identity and Authentication services whenever a user-facing
/// email should be sent. The Notifications service consumes this event and delivers
/// it through its email sender (a mock/in-memory implementation in development).
/// </summary>
/// <param name="UserId">The id of the user the email is addressed to.</param>
/// <param name="Email">The recipient email address.</param>
/// <param name="Subject">The email subject line.</param>
/// <param name="Message">The email body.</param>
public record SendEmailIntegrationEvent(
    Guid UserId,
    string Email,
    string Subject,
    string Message) : IntegrationEvent;
