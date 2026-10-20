using ShopIt.Framework.Core.Events.Integration;

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
    /// This service consumes the given events. Each is an <see cref="IntegrationEvent"/> type whose
    /// namespace-qualified name is the Kafka topic; the type — not a string — is the declaration, so the
    /// topic and the handler lookup cannot drift apart.
    /// </summary>
    /// <param name="eventTypes">One or more concrete integration event types.</param>
    /// <returns>The subscription.</returns>
    /// <exception cref="ArgumentException">No types were supplied, or one is not a concrete event.</exception>
    public static InboxSubscription Consuming(params Type[] eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        if (eventTypes.Length == 0)
        {
            throw new ArgumentException(
                "A consuming inbox needs at least one event type. Use InboxSubscription.None if this " +
                "service is publish-only.",
                nameof(eventTypes));
        }

        foreach (var eventType in eventTypes)
        {
            InboxEventRegistry.EnsureIsIntegrationEvent(eventType, nameof(eventTypes));
        }

        return new InboxSubscription(
            eventTypes
                .Select(InboxEventRegistry.EventTypeNameOf)
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Whether this service consumes anything.
    /// </summary>
    public bool IsConsuming => _topics.Count > 0;
}
