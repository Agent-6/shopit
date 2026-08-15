using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Roles;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.DataSeeding;

/// <summary>
/// Seeds a tenant's static roles (with their default permission claims) and an admin user
/// assigned to every role. Invoked when a tenant is created (via the tenant-created
/// integration event) and reusable for startup seeding.
/// </summary>
public class TenantDataSeeder(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ICurrentTenant currentTenant,
    IPermissionDefinitionProvider permissionCatalog,
    IRoleDefinitionProvider roleDefinitions,
    IOptions<SeedOptions> seedOptions,
    ILogger<TenantDataSeeder> logger) : ITenantDataSeeder
{
    public async Task SeedTenantAsync(Guid tenantId, string tenantName, CancellationToken cancellationToken)
    {
        using (currentTenant.Change(new TenantInfo(tenantId, tenantName)))
        {
            // 1. Provision the static roles + their default permission claims.
            foreach (var definition in roleDefinitions.GetAll())
            {
                await EnsureRoleAsync(definition, tenantId, cancellationToken);
            }

            // 2. Admin user, assigned to every role.
            var adminEmail = BuildAdminEmail(tenantName, tenantId);
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = User.Create(Guid.NewGuid(), adminEmail, adminEmail, tenantId, createdBy: "system");
                admin.SetPassword(new PasswordHasher<User>().HashPassword(admin, seedOptions.Value.AdminPassword));
                admin.ConfirmEmail();

                var result = await userManager.CreateAsync(admin);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed admin for tenant '{tenantName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }

                logger.LogInformation("Seeded tenant admin {Email} for tenant {TenantId}", adminEmail, tenantId);
            }

            foreach (var definition in roleDefinitions.GetAll())
            {
                // Only roles provisioned on this tenant's side exist here; assigning
                // a host-only role to a tenant user would fail role resolution.
                var tenantSide = tenantId == Guid.Empty
                    ? PermissionMultiTenancySide.Host
                    : PermissionMultiTenancySide.Tenant;

                if (!definition.Side.IsAvailableOn(tenantSide))
                {
                    continue;
                }

                if (!await userManager.IsInRoleAsync(admin, definition.Name))
                {
                    await userManager.AddToRoleAsync(admin, definition.Name);
                }
            }

            logger.LogInformation("Tenant {TenantId} ({Name}) fully provisioned.", tenantId, tenantName);
        }
    }

    private async Task EnsureRoleAsync(RoleDefinition definition, Guid tenantId, CancellationToken cancellationToken)
    {
        // Roles are only provisioned on the sides they are available on: a host-only
        // role definition is not created in tenant tenants and vice versa.
        var roleSide = tenantId == Guid.Empty
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;

        if (!definition.Side.IsAvailableOn(roleSide))
        {
            return;
        }

        var role = await roleManager.FindByNameAsync(definition.Name);
        if (role is null)
        {
            role = Role.Create(
                Guid.NewGuid(),
                definition.Name,
                tenantId,
                createdBy: "system",
                definition.Description,
                definition.Side);
            await roleManager.CreateAsync(role);
        }

        // Permissions are only grantable on the side they are available on. Admin
        // (DefaultPermissions == null) gets every permission available on this tenant's
        // side; other roles get their declared defaults filtered by side too.

        var catalog = permissionCatalog.GetAll().ToList();
        var toGrant = definition.GrantsAllPermissions
            ? catalog
                .Where(p => p.MultiTenancySide.IsAvailableOn(roleSide))
                .Select(p => p.Name.Value)
            : definition.DefaultPermissions!
                .Where(name => catalog.Any(p =>
                    p.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)
                    && p.MultiTenancySide.IsAvailableOn(roleSide)))
                .Select(p => p.Value);

        var granted = (await roleManager.GetClaimsAsync(role))
            .Select(c => c.Type)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in toGrant.Where(p => !granted.Contains(p)))
        {
            await roleManager.AddClaimAsync(role, new Claim(permission, "true"));
        }
    }

    private static string BuildAdminEmail(string tenantName, Guid tenantId)
    {
        var slug = string.Concat(
                tenantName.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'))
            .Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? $"admin@{tenantId:N}.local"
            : $"admin@{slug}.local";
    }
}
