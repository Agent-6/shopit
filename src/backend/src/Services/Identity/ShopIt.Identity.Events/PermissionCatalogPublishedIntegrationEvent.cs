using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Events;

/// <summary>
/// Published by every microservice to announce its permission catalog (the permission
/// groups and definitions the service exposes). The Identity service consumes it, upserts
/// the definitions into its persisted permission catalog, and grants any permissions the
/// Admin role does not already hold. Services republish their catalog whenever their
/// permission definitions change (on startup), so new permissions reach Identity without
/// redeploying the Identity project.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Owned by Identity, though published by others.</strong> Identity owns the meaning of the
/// message: it owns the permission registry, the synchronizer and the persistence, so
/// <em>"here is my catalog"</em> is a message <em>to</em> Identity. That is the ownership rule this
/// repository follows — the publisher owns a domain event, the receiving authority owns a directed
/// message.
/// </para>
/// <para>
/// <strong>Accepted consequence:</strong> a service that publishes a catalog takes a reference on
/// <c>ShopIt.Identity.Events</c>. That is a service→service reference, but it points downstream and
/// is deliberate — not an oversight. It is the price of a rule with no exceptions; the alternative
/// was a shared-kernel exception in <c>ShopIt.Framework.Core</c>.
/// </para>
/// </remarks>
/// <param name="SourceService">The name of the publishing service (e.g. "Tenancy").</param>
/// <param name="Groups">The permission groups and their permissions.</param>
public sealed record PermissionCatalogPublishedIntegrationEvent(
    string SourceService,
    IReadOnlyList<PermissionGroupDto> Groups) : IntegrationEvent;

/// <summary>Wire representation of a permission group in a catalog event.</summary>
public sealed record PermissionGroupDto(
    string Name,
    string DisplayName,
    IReadOnlyList<PermissionDefinitionDto> Permissions);

/// <summary>Wire representation of a single permission definition in a catalog event.</summary>
public sealed record PermissionDefinitionDto(
    string Name,
    string DisplayName,
    string? Description,
    PermissionMultiTenancySide MultiTenancySide = PermissionMultiTenancySide.Both);
