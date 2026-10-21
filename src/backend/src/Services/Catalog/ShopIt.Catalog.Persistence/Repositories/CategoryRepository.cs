using Microsoft.EntityFrameworkCore;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Catalog.Persistence.Data;
using ShopIt.Framework.Persistence.Repositories;

namespace ShopIt.Catalog.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/>.
/// </summary>
/// <remarks>
/// Every query goes through <c>DbSet</c>, so the tenant query filter is always in play: the existence
/// check is per tenant and a lookup by another tenant's id returns <c>null</c> rather than a row.
/// </remarks>
public class CategoryRepository(CatalogDbContext dbContext)
    : Repository<Category, Guid, CatalogDbContext>(dbContext), ICategoryRepository
{
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = Category.NormalizeName(name);

        return await DbSet.AnyAsync(
            category => category.NormalizedName == normalizedName,
            cancellationToken);
    }

    public async Task<(IEnumerable<Category> Items, long TotalCount, long TotalPages)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            // ILIKE, not LIKE: a catalog search that ignores case is what a user typing "tools" expects,
            // and PostgreSQL is the only provider this service targets.
            query = query.Where(category => EF.Functions.ILike(category.Name, $"%{filter}%"));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = (totalCount / pageSize) + (totalCount % pageSize > 0 ? 1 : 0);

        var categories = await query
            // Total order, so paging cannot show a row twice or skip it: names may repeat across
            // tenants and, after the normalized uniqueness rule, within a tenant only by id.
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (categories, totalCount, totalPages);
    }
}
