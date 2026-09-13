namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Default implementation of <see cref="IPermissionDefinitionContext"/> used by the
/// permission definition providers.
/// </summary>
public class PermissionDefinitionContext : IPermissionDefinitionContext
{
    private readonly List<PermissionGroupDefinition> _groups = [];
    private readonly Dictionary<PermissionGroupName, PermissionGroupDefinition> _byName = new();

    public PermissionGroupDefinition AddGroup(PermissionGroupName name, string displayName)
    {
        if (_byName.TryGetValue(name, out var existing))
            return existing;

        var group = new PermissionGroupDefinition(name, displayName);
        _groups.Add(group);
        _byName[name] = group;
        return group;
    }

    public PermissionDefinition AddPermission(
        PermissionGroupName groupName,
        PermissionName name,
        string displayName,
        string? description = null)
    {
        if (!_byName.TryGetValue(groupName, out var group))
            throw new InvalidOperationException(
                $"Permission group '{groupName}' must be added before adding permissions to it.");

        return group.Append(new PermissionDefinition(name, displayName, description));
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;
}
