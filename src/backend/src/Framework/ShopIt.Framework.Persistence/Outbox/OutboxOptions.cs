namespace ShopIt.Framework.Persistence.Outbox;

/// <summary>
/// Configuration options for the <see cref="OutboxProcessor{TContext}"/>.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers string (e.g. "localhost:9092").
    /// </summary>
    public string KafkaBootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the polling interval in milliseconds between batch processing runs.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the maximum number of outbox messages to process per batch.
    /// Default: 50.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets an optional function that maps an event type name to a Kafka topic name.
    /// When null, the event type name is used directly as the topic.
    /// </summary>
    public Func<string, string>? TopicResolver { get; set; }
}
