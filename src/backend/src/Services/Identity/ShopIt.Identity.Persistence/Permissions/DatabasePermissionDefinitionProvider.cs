using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// <see cref="IPermissionDefinitionProvider"/> backed by the persisted permission catalog.
/// The catalog is the union of every service's definitions (Identity seeds its own at
/// startup; other services publish theirs via integration events), so the grantable
/// permission set updates without redeploying this service. Replaces the previous
/// in-memory provider, which could only see permissions hardcoded into the Identity
/// codebase.
/// </summary>
public class DatabasePermissionDefinitionProvider(ApplicationDbContext dbContext) : IPermissionDefinitionProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public void Define(IPermissionDefinitionContext context)
    {
        throw new NotSupportedException(
            "The persisted permission catalog is read-only; it is populated from catalogs " +
            "published by each service. Use a service-specific provider (e.g. " +
            nameof(ShopIt.Identity.Domain.Permissions.ShopItIdentityPermissionDefinitionProvider) +
            ") for defining new permissions.");
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups()
    {
        var entries = _dbContext.PermissionCatalogEntries
            .OrderBy(e => e.GroupName)
            .ThenBy(e => e.Name)
            .ToList();

        return entries
            .GroupBy(e => new { e.GroupName, e.GroupDisplayName })
            .Select(g => new PermissionGroupDefinition(
                new PermissionGroupName(g.Key.GroupName),
                g.Key.GroupDisplayName,
                g.Select(e => new PermissionDefinition(
                    new PermissionName(e.Name),
                    e.DisplayName,
                    e.Description,
                    e.MultiTenancySide)).ToList()))
            .ToList();
    }

    public IEnumerable<PermissionDefinition> GetAll() =>
        GetGroups().SelectMany(g => g.Permissions);
}
