using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Core.Events.Integration;

/// <summary>
/// Base record for all integration events that are published across service boundaries via Kafka.
/// Integration events are written to the transactional outbox and consumed via the inbox pattern.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Gets the unique identifier for this event instance.</summary>
    public Guid EventId { get; } = DomainProviders.Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    public DateTime OccurredOn { get; } = DomainProviders.Date.UtcNow;

    /// <summary>
    /// Gets the wire name of the event: its namespace-qualified type name. Used as the Kafka topic and as
    /// the message type discriminator.
    /// </summary>
    /// <remarks>
    /// Namespace-qualified rather than the simple type name, because a simple name is not an identity:
    /// two events named the same in different namespaces would be indistinguishable on the wire, and the
    /// consumer resolves the header back to a CLR type to deserialize it.
    /// </remarks>
    public string EventType => GetType().FullName
        ?? throw new InvalidOperationException(
            $"Integration event '{GetType()}' has no full name and cannot be addressed on the wire.");
}
