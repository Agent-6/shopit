namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// Represents an inbox message received from Kafka.
/// Used to detect and reject duplicate messages before dispatching to handlers,
/// implementing the Idempotent Consumer pattern.
/// </summary>
public class InboxMessage
{
    /// <summary>Gets or sets the unique identifier, sourced from the integration event's EventId.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the event type name (e.g. "UserRegisteredIntegrationEvent").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized integration event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when this message was received from Kafka.</summary>
    public DateTime ReceivedOn { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this message was successfully processed. Null if not yet processed.</summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>Gets or sets an error message if processing failed on the last attempt.</summary>
    public string? Error { get; set; }
}
