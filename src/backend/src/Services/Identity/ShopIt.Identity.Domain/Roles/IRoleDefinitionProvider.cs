namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Supplies the catalog of built-in roles and the default permission keys each role
/// is granted. Consumed by the seed services to provision roles for hosts and tenants.
/// </summary>
public interface IRoleDefinitionProvider
{
    /// <summary>
    /// Returns every built-in role definition.
    /// </summary>
    IReadOnlyList<RoleDefinition> GetAll();
}
