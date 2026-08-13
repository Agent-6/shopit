namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Groups related permissions together so they can be presented as a section
/// (e.g. "User Management") in permission management UIs.
/// </summary>
public class PermissionGroupDefinition
{
    public string Name { get; }
    public string DisplayName { get; }
    public IReadOnlyList<PermissionDefinition> Permissions { get; }

    public PermissionGroupDefinition(string name, string displayName, IEnumerable<PermissionDefinition> permissions)
    {
        Name = name;
        DisplayName = displayName;
        Permissions = permissions.ToList();
    }
}
