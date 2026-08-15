using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Permissions;

/// <inheritdoc cref="IPermissionCatalogSynchronizer" />
public class PermissionCatalogSynchronizer(
    ApplicationDbContext dbContext,
    ILogger<PermissionCatalogSynchronizer> logger) : IPermissionCatalogSynchronizer
{
    // The Admin role is granted every permission as it enters the catalog. Role names are
    // tenant-scoped, so this matches the host role and every tenant's Admin role.
    private const string AdminRoleNormalizedName = "ADMIN";

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<PermissionCatalogSynchronizer> _logger = logger;

    public async Task SynchronizeAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default)
    {
        var existingByName = await _dbContext.PermissionCatalogEntries
            .ToDictionaryAsync(e => e.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        // The new publication is the authoritative set for this service: any of its catalog
        // entries not present here have been removed and must be deleted (with their grants).
        var publishedNames = groups
            .SelectMany(g => g.Permissions)
            .Select(p => p.Name.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removedEntries = existingByName.Values
            .Where(e => string.Equals(e.SourceService, sourceService, StringComparison.OrdinalIgnoreCase)
                        && !publishedNames.Contains(e.Name))
            .ToList();

        var addedNames = new List<string>();
        var addedSides = new Dictionary<string, PermissionMultiTenancySide>(StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        foreach (var group in groups)
        {
            foreach (var permission in group.Permissions)
            {
                var name = permission.Name.Value;

                if (existingByName.TryGetValue(name, out var entry))
                {
                    // The permission identity (name) is immutable; refresh its metadata.
                    entry.Update(
                        group.Name,
                        group.DisplayName,
                        permission.DisplayName,
                        permission.Description,
                        sourceService,
                        permission.MultiTenancySide);
                    updated++;
                }
                else
                {
                    _dbContext.PermissionCatalogEntries.Add(PermissionCatalogEntry.Create(
                        Guid.NewGuid(),
                        group.Name,
                        group.DisplayName,
                        name,
                        permission.DisplayName,
                        permission.Description,
                        sourceService,
                        permission.MultiTenancySide));
                    addedNames.Add(name);
                    addedSides[name] = permission.MultiTenancySide;
                }
            }
        }

        var removedNames = removedEntries.Select(e => e.Name).ToList();
        var revoked = 0;

        if (removedEntries.Count > 0)
        {
            // Delete the catalog entries this service no longer publishes.
            _dbContext.PermissionCatalogEntries.RemoveRange(removedEntries);

            // Revoke the removed permissions from every role that holds them (Admin's
            // auto-granted claims included), across all tenants. Claims for permissions that
            // no longer exist in the catalog would otherwise dangle and break role saves.
            var claimsToRemove = await _dbContext.RoleClaims
                .IgnoreQueryFilters()
                .Where(c => c.ClaimType != null && removedNames.Contains(c.ClaimType))
                .ToListAsync(cancellationToken);

            if (claimsToRemove.Count > 0)
            {
                _dbContext.RoleClaims.RemoveRange(claimsToRemove);
                revoked = claimsToRemove.Count;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var granted = addedNames.Count == 0
            ? 0
            : await GrantToAdminRolesAsync(addedSides, cancellationToken);

        _logger.LogInformation(
            "Permission catalog synchronized from '{SourceService}': {Added} added, {Updated} updated, " +
            "{Removed} removed ({Revoked} grant(s) revoked), {Granted} new permission(s) granted to Admin role(s).",
            sourceService, addedNames.Count, updated, removedNames.Count, revoked, granted);
    }

    /// <summary>
    /// Grants the given permission names to the Admin role in every tenant (host + all
    /// tenants), skipping grants that already exist and permissions not available on the
    /// role's multi-tenancy side (host Admin gets Host/Both, tenant Admins get
    /// Tenant/Both). Returns the number of claims added.
    /// </summary>
    private async Task<int> GrantToAdminRolesAsync(
        IReadOnlyDictionary<string, PermissionMultiTenancySide> newPermissions,
        CancellationToken cancellationToken)
    {
        var adminRoles = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.NormalizedName == AdminRoleNormalizedName)
            .ToListAsync(cancellationToken);

        if (adminRoles.Count == 0)
        {
            _logger.LogDebug(
                "No Admin role exists yet; skipping grants for {Count} new permission(s). " +
                "Role seeding grants the full catalog when the role is created.",
                newPermissions.Count);
            return 0;
        }

        var existingClaims = await _dbContext.RoleClaims
            .IgnoreQueryFilters()
            .Where(c => newPermissions.Keys.Contains(c.ClaimType))
            .Select(c => new { c.RoleId, c.ClaimType })
            .ToListAsync(cancellationToken);

        var alreadyGranted = existingClaims
            .Select(c => (c.RoleId, c.ClaimType))
            .ToHashSet();

        var granted = 0;
        foreach (var role in adminRoles)
        {
            var roleSide = role.TenantId == Guid.Empty
                ? PermissionMultiTenancySide.Host
                : PermissionMultiTenancySide.Tenant;

            foreach (var (name, side) in newPermissions)
            {
                if (!side.IsAvailableOn(roleSide))
                {
                    continue;
                }

                if (alreadyGranted.Contains((role.Id, name)))
                {
                    continue;
                }

                _dbContext.RoleClaims.Add(RoleClaim.Create(role, name, "true"));
                granted++;
            }
        }

        if (granted > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return granted;
    }
}
