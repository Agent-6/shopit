using ShopIt.Identity.Domain.Roles;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the role definitions that <c>PermissionResolver</c> treats as all-permissions.
/// </summary>
/// <remarks>
/// <para>
/// The resolver grants every permission in the catalog to a holder of a role that is <em>both</em>
/// built-in (<c>Role.IsStatic</c>) and defined with <c>GrantsAllPermissions</c>. Those definitions are
/// declared in one place, and a single mistake there — giving a role a null
/// <c>DefaultPermissions</c>, which is how "grants everything" is expressed — would silently promote
/// every holder of that role, in every tenant, to full access.
/// </para>
/// <para>
/// Hence this test: the set of all-permissions roles is asserted, not assumed.
/// </para>
/// </remarks>
public class RoleDefinitionTests
{
    private static readonly IReadOnlyList<RoleDefinition> Definitions =
        new ShopItIdentityRoleDefinitionProvider().GetAll();

    [Fact]
    public void Exactly_one_built_in_role_grants_every_permission_and_it_is_Admin()
    {
        var granting = Definitions.Where(d => d.GrantsAllPermissions).ToList();

        var admin = Assert.Single(granting);
        Assert.Equal("Admin", admin.Name.Value);
    }

    [Fact]
    public void An_all_permissions_role_must_be_static()
    {
        // Both halves of the resolver's rule are load-bearing. A role that grants everything but is not
        // static would be skipped by the resolver — and, worse, the intent would be invisible.
        Assert.All(
            Definitions.Where(d => d.GrantsAllPermissions),
            d => Assert.True(d.IsStatic, $"'{d.Name.Value}' grants everything but is not static."));
    }

    [Fact]
    public void Other_built_in_roles_declare_their_permissions_explicitly()
    {
        // A null DefaultPermissions is the all-permissions signal, so every other role must be explicit.
        // This is the test that catches an accidental promotion.
        var implicitGrant = Definitions
            .Where(d => d.Name.Value != "Admin")
            .Where(d => d.DefaultPermissions is null)
            .Select(d => d.Name.Value)
            .ToList();

        Assert.Empty(implicitGrant);
    }

    [Fact]
    public void Every_definition_has_a_unique_name()
    {
        var duplicates = Definitions
            .GroupBy(d => d.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        Assert.Empty(duplicates);
    }
}
