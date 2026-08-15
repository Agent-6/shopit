using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Core.Events.Integration;

/// <summary>
/// Published by every microservice to announce its permission catalog (the permission
/// groups and definitions the service exposes). The Identity service consumes it, upserts
/// the definitions into its persisted permission catalog, and grants any permissions the
/// Admin role does not already hold. Services republish their catalog whenever their
/// permission definitions change (on startup), so new permissions reach Identity without
/// redeploying the Identity project.
/// </summary>
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
