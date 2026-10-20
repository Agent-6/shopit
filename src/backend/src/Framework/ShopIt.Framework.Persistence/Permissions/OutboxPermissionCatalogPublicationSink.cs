using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Default sink: publishes the catalog as a <c>PermissionCatalogPublishedIntegrationEvent</c>
/// through the outbox, so it reaches the Identity service over Kafka like any other message.
/// </summary>
/// <remarks>
/// Chosen for services that do not own the permission registry. The event is written and
/// committed in one <c>SaveChangesAsync</c>, so the outbox row and the migration-completed
/// database are consistent: either the catalog is queued for delivery or startup fails.
/// </remarks>
/// <typeparam name="TContext">The service's DbContext, which owns the outbox table.</typeparam>
public sealed class OutboxPermissionCatalogPublicationSink<TContext>(
    IOutboxWriter outboxWriter,
    TContext dbContext,
    PermissionCatalogPublishingOptions options,
    ILogger<OutboxPermissionCatalogPublicationSink<TContext>> logger)
    : IPermissionCatalogPublicationSink
    where TContext : DbContext
{
    /// <inheritdoc />
    public async Task PublishAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default)
    {
        var createEvent = options.CreateEvent
            ?? throw new InvalidOperationException(
                "No permission-catalog event factory is registered. Register the hook with the " +
                "overload that takes a CreateEvent factory, or register a custom " +
                $"{nameof(IPermissionCatalogPublicationSink)}.");

        var integrationEvent = createEvent(sourceService, groups);

        await outboxWriter.WriteAsync(integrationEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Queued permission catalog from '{SourceService}' in the outbox ({GroupCount} group(s), {PermissionCount} permission(s)).",
            sourceService,
            groups.Count,
            groups.Sum(g => g.Permissions.Count));
    }
}
