namespace ShopIt.Notifications.Application.Emails;

/// <summary>
/// Options for the SMTP email sender. Host and port are normally derived from the
/// Mailpit connection string injected by Aspire; the <c>Email:Smtp</c> configuration
/// section can override them (or point at a real server in production).
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    /// <summary>SMTP server hostname.</summary>
    public string? Host { get; set; }

    /// <summary>SMTP server port.</summary>
    public int? Port { get; set; }

    /// <summary>The From address stamped on outgoing emails.</summary>
    public string From { get; set; } = "no-reply@shopit.local";

    /// <summary>Optional SMTP username (Mailpit requires none).</summary>
    public string? UserName { get; set; }

    /// <summary>Optional SMTP password (Mailpit requires none).</summary>
    public string? Password { get; set; }
}
