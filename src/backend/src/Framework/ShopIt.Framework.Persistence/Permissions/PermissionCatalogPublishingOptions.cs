using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Registration state shared by the permission-catalog publishing hook: which service the
/// catalog belongs to, and how to turn that catalog into an integration event.
/// </summary>
public sealed class PermissionCatalogPublishingOptions
{
    /// <summary>
    /// The name of the service publishing the catalog (e.g. <c>"Tenancy"</c>). Stamped on
    /// every catalog entry so a republished catalog replaces only its own service's entries.
    /// </summary>
    public required string SourceService { get; init; }

    /// <summary>
    /// Maps (source service, groups) to the catalog integration event. Required by the outbox
    /// sink; not used when the service synchronizes its own catalog in place.
    /// </summary>
    public Func<string, IReadOnlyList<PermissionGroupDefinition>, IntegrationEvent>? CreateEvent { get; init; }
}
