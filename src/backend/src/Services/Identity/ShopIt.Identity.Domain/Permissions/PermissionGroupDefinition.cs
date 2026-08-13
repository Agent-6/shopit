namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Groups related permissions together so they can be presented as a section
/// (e.g. "User Management") in permission management UIs. Keyed by a
/// <see cref="PermissionGroupName"/> value object.
/// </summary>
public class PermissionGroupDefinition
{
    public PermissionGroupName Name { get; }
    public string DisplayName { get; }

    private readonly List<PermissionDefinition> _permissions = [];
    public IReadOnlyList<PermissionDefinition> Permissions => _permissions;

    public PermissionGroupDefinition(
        PermissionGroupName name,
        string displayName,
        IEnumerable<PermissionDefinition>? permissions = null)
    {
        Name = name;
        DisplayName = displayName;
        if (permissions is not null)
            _permissions.AddRange(permissions);
    }

    /// <summary>
    /// Appends a permission to the group. Mutations flow through the fluent
    /// <c>AddPermission</c> extension methods on <see cref="PermissionGroupDefinition"/>
    /// and <see cref="PermissionName"/>.
    /// </summary>
    internal PermissionDefinition Append(PermissionDefinition permission)
    {
        _permissions.Add(permission);
        return permission;
    }
}
