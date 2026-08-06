namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// Configuration options for the <see cref="InboxProcessor{TContext}"/>.
/// </summary>
public sealed class InboxOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers string (e.g. "localhost:9092").
    /// </summary>
    public string KafkaBootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the Kafka consumer group ID.
    /// </summary>
    public string ConsumerGroupId { get; set; } = "shopit-inbox";

    /// <summary>
    /// Gets or sets the list of Kafka topics to subscribe to.
    /// </summary>
    public List<string> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum time to wait for a Kafka message before looping (milliseconds).
    /// Default: 2000ms.
    /// </summary>
    public int ConsumeTimeoutMs { get; set; } = 2000;
}
