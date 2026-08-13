namespace ShopIt.Identity.Application.Notifications;

/// <summary>
/// Options for building user-facing email messages. The activation and password-reset
/// links point at the Authentication service's MVC pages, so the Identity service needs
/// its public base URL to build absolute links in the notification messages.
/// </summary>
public sealed class EmailNotificationOptions
{
    public const string SectionName = "EmailNotifications";

    /// <summary>
    /// Gets or sets the public base URL of the Authentication service
    /// (e.g. <c>https://localhost:7234</c>).
    /// </summary>
    public string AuthBaseUrl { get; set; } = "https://localhost:7234";
}
