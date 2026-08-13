using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Permissions;
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
        var role = await roleManager.FindByNameAsync(definition.Name);
        if (role is null)
        {
            role = Role.Create(
                Guid.NewGuid(),
                definition.Name,
                tenantId,
                createdBy: "system",
                definition.Description);
            await roleManager.CreateAsync(role);
        }

        var toGrant = definition.GrantsAllPermissions
            ? permissionCatalog.GetAll().Select(p => p.Name.Value)
            : definition.DefaultPermissions!.Select(p => p.Value);

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
