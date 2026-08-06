using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Persistence.Outbox;

namespace ShopIt.Framework.Infrastructure.Events;

/// <summary>
/// Implements <see cref="IOutboxWriter"/> by persisting integration events as
/// <see cref="OutboxMessage"/> rows in the same <see cref="DbContext"/> that is
/// handling the current request. Because the writer participates in the active
/// EF Core <see cref="DbContext"/>, the outbox write is committed atomically
/// with the domain aggregate changes.
/// </summary>
public class OutboxWriter(DbContext dbContext, ILogger<OutboxWriter> logger) : IOutboxWriter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly DbContext _dbContext = dbContext;
    private readonly ILogger<OutboxWriter> _logger = logger;

    /// <inheritdoc />
    public async Task WriteAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _jsonOptions);

        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = payload,
            OccurredOn = integrationEvent.OccurredOn,
        };

        _dbContext.Set<OutboxMessage>().Add(message);

        _logger.LogDebug(
            "Enqueued outbox message {EventType} (EventId: {EventId})",
            integrationEvent.EventType, integrationEvent.EventId);

        await Task.CompletedTask;
    }
}
