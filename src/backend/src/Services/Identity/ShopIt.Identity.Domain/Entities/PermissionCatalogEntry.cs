using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Domain.Entities;

/// <summary>
/// A single permission definition persisted in the Identity service's permission catalog.
/// The catalog is the union of every microservice's permission definitions: each service
/// publishes its catalog via an integration event and Identity upserts it here, so new
/// permissions arrive without redeploying Identity. The catalog is system-wide (not
/// tenant-scoped) — every tenant sees the same grantable permissions.
/// </summary>
public class PermissionCatalogEntry : IEntity<Guid>
{
    public Guid Id { get; private set; } = default!;
    public string GroupName { get; private set; } = default!;
    public string GroupDisplayName { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Description { get; private set; }
    public string SourceService { get; private set; } = default!;
    public PermissionMultiTenancySide MultiTenancySide { get; private set; } = PermissionMultiTenancySide.Both;
    public DateTime CreatedAt { get; private set; } = default!;
    public DateTime? UpdatedAt { get; private set; }

    public object GetId() => Id;

    // Public parameterless constructor for EF Core.
    public PermissionCatalogEntry() { }

    private PermissionCatalogEntry(
        Guid id,
        string groupName,
        string groupDisplayName,
        string name,
        string displayName,
        string? description,
        string sourceService,
        PermissionMultiTenancySide multiTenancySide)
    {
        Id = id;
        GroupName = groupName;
        GroupDisplayName = groupDisplayName;
        Name = name;
        DisplayName = displayName;
        Description = description;
        SourceService = sourceService;
        MultiTenancySide = multiTenancySide;
        CreatedAt = DateTime.UtcNow;
    }

    public static PermissionCatalogEntry Create(
        Guid id,
        string groupName,
        string groupDisplayName,
        string name,
        string displayName,
        string? description,
        string sourceService,
        PermissionMultiTenancySide multiTenancySide = PermissionMultiTenancySide.Both)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty or whitespace.", nameof(name));

        return new PermissionCatalogEntry(
            id, groupName, groupDisplayName, name, displayName, description, sourceService, multiTenancySide);
    }

    /// <summary>
    /// Refreshes the display metadata from a republished definition. The permission
    /// identity (<see cref="Name"/>) is immutable once persisted.
    /// </summary>
    public void Update(
        string groupName,
        string groupDisplayName,
        string displayName,
        string? description,
        string sourceService,
        PermissionMultiTenancySide multiTenancySide = PermissionMultiTenancySide.Both)
    {
        GroupName = groupName;
        GroupDisplayName = groupDisplayName;
        DisplayName = displayName;
        Description = description;
        SourceService = sourceService;
        MultiTenancySide = multiTenancySide;
        UpdatedAt = DateTime.UtcNow;
    }
}
