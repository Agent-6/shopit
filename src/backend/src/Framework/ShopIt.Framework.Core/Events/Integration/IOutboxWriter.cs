namespace ShopIt.Framework.Core.Events.Integration;

/// <summary>
/// Writes integration events to the transactional outbox.
/// Must be called within an active database transaction so the outbox message
/// is committed atomically with the domain changes.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Enqueues the given integration event into the outbox table.
    /// </summary>
    /// <param name="integrationEvent">The integration event to enqueue.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task WriteAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
