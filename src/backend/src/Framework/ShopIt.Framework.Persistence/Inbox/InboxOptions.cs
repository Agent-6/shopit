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
    /// Gets or sets the Kafka consumer group ID. Set from the <c>consumerGroupId</c> argument of
    /// <see cref="DependencyInjection.AddKafkaIntegration{TContext}"/>, which requires it, so a service
    /// cannot inherit another service's group by defaulting.
    /// </summary>
    /// <remarks>
    /// Kafka delivers each partition of a topic to exactly one consumer <em>within</em> a group. A
    /// shared group id therefore means two services subscribed to the same topic each receive only
    /// some of its messages, with the split decided by partition assignment — silent and
    /// dependent on startup order.
    /// </remarks>
    public string ConsumerGroupId { get; set; } = string.Empty;

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
