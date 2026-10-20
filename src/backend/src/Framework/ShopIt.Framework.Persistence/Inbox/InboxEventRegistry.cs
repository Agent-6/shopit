using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// The inbox's event-type lookup: the mapping from a message's <c>EventType</c> discriminator to the
/// CLR type to deserialize it into, built once at registration from the handlers the service actually
/// has.
/// </summary>
/// <remarks>
/// <para>
/// The inbox used to resolve this by scanning every loaded assembly for a type whose <em>simple</em>
/// name matched the message header. That had two failure modes: two events named the same in different
/// namespaces collided with the winner decided by assembly load order, and the set of "available" types
/// depended on what happened to be loaded when the message arrived.
/// </para>
/// <para>
/// The registry replaces the scan with a fact established at startup: these are the events this service
/// has handlers for, keyed by their namespace-qualified name. Dispatch is then a dictionary lookup —
/// deterministic, independent of load order, and impossible to widen by loading another assembly.
/// </para>
/// </remarks>
public sealed class InboxEventRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _eventTypesByName;
    private readonly Type[] _eventTypes;

    /// <summary>
    /// Creates a registry from the event types a service has handlers for.
    /// </summary>
    /// <param name="eventTypes">The event types, each with at least one registered handler.</param>
    /// <exception cref="InvalidOperationException">
    /// Two distinct types share a namespace-qualified name, which would make the discriminator ambiguous.
    /// </exception>
    public InboxEventRegistry(IEnumerable<Type> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        var byName = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var eventType in eventTypes.Distinct())
        {
            var name = EventTypeNameOf(eventType);

            if (byName.TryGetValue(name, out var existing) && existing != eventType)
            {
                throw new InvalidOperationException(
                    $"Integration events '{existing.FullName}' and '{eventType.FullName}' share the wire " +
                    $"name '{name}'. Give one of them a distinct namespace so the inbox can tell them apart.");
            }

            byName[name] = eventType;
        }

        _eventTypesByName = byName;
        _eventTypes = [.. byName.Values];
    }

    /// <summary>
    /// The event types this service can dispatch. Handlers for anything else cannot be registered,
    /// because registrations are what build this registry.
    /// </summary>
    public IReadOnlyCollection<Type> EventTypes => _eventTypes;

    /// <summary>
    /// Resolves the CLR type for a message's <c>EventType</c> discriminator.
    /// </summary>
    /// <param name="eventTypeName">The discriminator from the message header.</param>
    /// <param name="eventType">The matching event type, if any.</param>
    /// <returns><c>true</c> when the event is known to this service.</returns>
    public bool TryResolve(string eventTypeName, out Type? eventType) =>
        _eventTypesByName.TryGetValue(eventTypeName, out eventType);

    /// <summary>
    /// The wire name of an event type — what the outbox writes to the message header and what the inbox
    /// looks up. Namespace-qualified so two same-named events never collide.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The wire name.</returns>
    public static string EventTypeNameOf(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return eventType.FullName
            ?? throw new InvalidOperationException(
                $"Integration event '{eventType}' has no full name and cannot be addressed on the wire.");
    }

    internal static void EnsureIsIntegrationEvent(Type eventType, string parameterName)
    {
        if (!typeof(IntegrationEvent).IsAssignableFrom(eventType) || eventType.IsAbstract)
        {
            throw new ArgumentException(
                $"'{eventType.FullName}' is not a concrete {nameof(IntegrationEvent)}.",
                parameterName);
        }
    }
}
