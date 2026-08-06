namespace ShopIt.Framework.Persistence.Outbox;

/// <summary>
/// Represents an outbox message persisted to the database.
/// Integration events are written here within the same DB transaction as the domain change,
/// then published to Kafka asynchronously by the <see cref="OutboxProcessor"/>.
/// </summary>
public class OutboxMessage
{
    /// <summary>Gets or sets the unique identifier, sourced from <see cref="Core.Events.Integration.IntegrationEvent.EventId"/>.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the event type name (e.g. "UserRegisteredIntegrationEvent").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized integration event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the event occurred.</summary>
    public DateTime OccurredOn { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this message was successfully published to Kafka. Null if not yet processed.</summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>Gets or sets an error message if publishing failed on the last attempt.</summary>
    public string? Error { get; set; }
}
