namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Canonical role names for the Identity service, as <see cref="RoleName"/> value objects.
/// </summary>
public static class ShopItIdentityRoles
{
    public static readonly RoleName Admin = new("Admin");
    public static readonly RoleName Manager = new("Manager");
    public static readonly RoleName User = new("User");
}
