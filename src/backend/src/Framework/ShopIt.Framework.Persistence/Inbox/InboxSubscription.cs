namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// Whether, and what, a service consumes from Kafka — declared where the inbox is registered, so
/// "this service is publish-only" is a statement rather than an omission.
/// </summary>
/// <remarks>
/// <para>
/// Before this type existed, a service with no inbox configuration registered an inbox that started,
/// logged a warning nobody read, and exited. That made two very different situations look identical in
/// logs and in the running system: a service deliberately publish-only, and a service whose author
/// forgot to configure consumption.
/// </para>
/// <para>
/// Choose one explicitly:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="None"/> — this service publishes and consumes nothing. No inbox processor is registered.</description></item>
/// <item><description><see cref="Consuming"/> — this service consumes the named topics. At least one topic is required, and every topic must have a registered handler.</description></item>
/// </list>
/// </remarks>
public sealed class InboxSubscription
{
    private readonly IReadOnlyList<string> _topics;

    private InboxSubscription(IReadOnlyList<string> topics)
    {
        _topics = topics;
    }

    /// <summary>
    /// This service does not consume integration events.
    /// </summary>
    public static InboxSubscription None { get; } = new([]);

    /// <summary>
    /// The topics this service consumes. Empty for <see cref="None"/>.
    /// </summary>
    public IReadOnlyList<string> Topics => _topics;

    /// <summary>
    /// This service consumes the given topics. Topics are integration event type names, which is what
    /// the outbox publishes as the message's <c>EventType</c> header.
    /// </summary>
    /// <param name="topics">One or more topic (event type) names.</param>
    /// <returns>The subscription.</returns>
    /// <exception cref="ArgumentException">No topics were supplied.</exception>
    public static InboxSubscription Consuming(params string[] topics)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if (topics.Length == 0)
        {
            throw new ArgumentException(
                "A consuming inbox needs at least one topic. Use InboxSubscription.None if this " +
                "service is publish-only.",
                nameof(topics));
        }

        if (topics.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Topic names cannot be empty or whitespace.", nameof(topics));
        }

        return new InboxSubscription(topics.Distinct(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Whether this service consumes anything.
    /// </summary>
    public bool IsConsuming => _topics.Count > 0;
}
