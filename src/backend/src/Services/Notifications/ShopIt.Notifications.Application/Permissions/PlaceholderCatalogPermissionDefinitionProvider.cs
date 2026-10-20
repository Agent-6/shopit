using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Notifications.Application.Permissions;

/// <summary>
/// **PLACEHOLDER (#24).** Declares <c>catalog.view</c> and <c>catalog.write</c> so the permission
/// recipe can be proven end to end before the product catalog service exists (M1): declared here,
/// published by the framework hook, persisted by Identity, granted to Admin, and enforced by
/// <c>RequirePermission("catalog.write")</c>.
/// </summary>
/// <remarks>
/// <para>
/// It lives in Notifications because Notifications is the only service that references no
/// <c>ShopIt.Identity.*</c> assembly other than the directed <c>.Events</c> contract every publisher
/// needs — which is what the issue set out to demonstrate.
/// </para>
/// <para>
/// <see cref="SourceService"/> is <c>"Catalog"</c>, not <c>"Notifications"</c>, so that ownership of
/// these two names transfers cleanly when the real catalog service publishes them: the synchronizer
/// keys entries by permission name, and a republished catalog only removes entries carrying its own
/// source service.
/// </para>
/// <para>
/// When M1 lands, delete this provider and its registration in <c>Notifications.API/Program.cs</c>.
/// That is the whole "replaces the placeholder" step — the publishing recipe needs no change.
/// </para>
/// </remarks>
public sealed class PlaceholderCatalogPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <summary>Source-service name stamped on these entries in Identity's catalog.</summary>
    public const string SourceService = "Catalog";

    private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

    public PlaceholderCatalogPermissionDefinitionProvider()
    {
        var context = new PermissionDefinitionContext();
        Define(context);
        _groups = context.GetGroups();
    }

    /// <inheritdoc />
    public void Define(IPermissionDefinitionContext context)
    {
        // Both sides: a catalog service will serve host and tenant requests alike, exactly as
        // Tenancy's view/update permissions do.
        PlaceholderCatalogPermissions.Catalog
            .AddGroup(context, "Catalog")
            .AddPermission(
                PlaceholderCatalogPermissions.View,
                "View catalog",
                "Browse products in the catalog.")
            .AddPermission(
                PlaceholderCatalogPermissions.Write,
                "Manage catalog",
                "Create, edit and remove products in the catalog.");
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    /// <inheritdoc />
    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(group => group.Permissions);
}
