using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the role-provenance invariant introduced so that "this is the built-in Admin" is a fact
/// about the row rather than an inference from a name a caller can choose.
/// </summary>
/// <remarks>
/// <para>
/// This is the half that needs no DI. The validator rules — create-time uniqueness, and the readable
/// rejection of a rename — depend on <c>RoleManager</c> and are not covered here.
/// </para>
/// <para>
/// <strong>Note the setup below.</strong> Domain entities raise domain events, and <c>DomainEvent</c>
/// reads <c>DomainProviders.Guid</c>/<c>Date</c> — static state that is <c>null</c> until DI populates
/// it via <c>UseDomainServices</c>, which is only reachable with the providers registered. So a plain
/// unit test cannot construct an entity without booting that state first. Worth knowing: it means no
/// domain entity is testable in isolation today.
/// </para>
/// </remarks>
public class RoleProvenanceTests
{
    static RoleProvenanceTests()
    {
        // Idempotent, write-once global state; xunit may run classes in parallel but this is safe.
        var services = new ServiceCollection();
        services.AddSingleton<IGuidProvider, TestGuidProvider>();
        services.AddSingleton<IDateProvider, TestDateProvider>();
        services.BuildServiceProvider().UseDomainServices();
    }

    /// <summary>
    /// Minimal stand-ins: the shipped implementations live in <c>Framework.Infrastructure</c> and are
    /// internal, and these interfaces exist precisely so tests can supply their own.
    /// </summary>
    private sealed class TestGuidProvider : IGuidProvider
    {
        public Guid NewGuid() => Guid.NewGuid();
    }

    private sealed class TestDateProvider : IDateProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Today => DateTime.UtcNow.Date;
    }

    private static readonly Guid TenantId = Guid.Parse("b5d0c0e4-3a5b-4cdc-8d2a-7f1f6c9f5b4e");

    private static Role NewRole(string name, bool isStatic) =>
        Role.Create(Guid.NewGuid(), name, TenantId, "test", isStatic: isStatic);

    [Fact]
    public void Static_role_cannot_be_renamed()
    {
        var role = NewRole("Admin", isStatic: true);

        var ex = Assert.Throws<InvalidOperationException>(() => role.Update("Admin2", null));

        Assert.Contains("cannot be renamed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Admin", role.Name);
        Assert.Equal("ADMIN", role.NormalizedName);
    }

    [Fact]
    public void Static_role_can_still_have_its_description_changed()
    {
        // The name is frozen, not the role. Otherwise the built-in roles would be uneditable.
        var role = NewRole("Admin", isStatic: true);

        role.Update("Admin", "Updated description");

        Assert.Equal("Admin", role.Name);
        Assert.Equal("Updated description", role.Description);
    }

    [Fact]
    public void Static_role_accepts_an_identical_name_regardless_of_case()
    {
        // Seeding re-applies the definition on every startup, so an update with the same name must
        // be a no-op rather than a failure.
        var role = NewRole("Admin", isStatic: true);

        role.Update("Admin", null);

        Assert.Equal("Admin", role.Name);
    }

    [Fact]
    public void Non_static_role_can_be_renamed()
    {
        var role = NewRole("Support", isStatic: false);

        role.Update("Support Team", null);

        Assert.Equal("Support Team", role.Name);
        Assert.Equal("SUPPORT TEAM", role.NormalizedName);
    }

    [Fact]
    public void MarkAsStatic_is_idempotent_and_then_freezes_the_name()
    {
        var role = NewRole("Admin", isStatic: false);
        Assert.False(role.IsStatic);

        role.MarkAsStatic();
        role.MarkAsStatic();

        Assert.True(role.IsStatic);
        Assert.Throws<InvalidOperationException>(() => role.Update("Admin2", null));
    }

    [Fact]
    public void Roles_are_not_static_by_default()
    {
        // A user-created role must never be static; only seeding sets it.
        var role = Role.Create(Guid.NewGuid(), "Support", TenantId, "test");

        Assert.False(role.IsStatic);
    }

    [Fact]
    public void Create_carries_the_multi_tenancy_side_and_static_flag_together()
    {
        var role = Role.Create(
            Guid.NewGuid(), "Admin", TenantId, "test",
            multiTenancySide: PermissionMultiTenancySide.Tenant,
            isStatic: true);

        Assert.True(role.IsStatic);
        Assert.Equal(PermissionMultiTenancySide.Tenant, role.MultiTenancySide);
    }
}
