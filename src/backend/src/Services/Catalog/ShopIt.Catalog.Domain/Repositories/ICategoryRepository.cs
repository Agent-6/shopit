using ShopIt.Catalog.Domain.Categories;
using ShopIt.Framework.Domain.Repositories;

namespace ShopIt.Catalog.Domain.Repositories;

/// <summary>
/// The repository contract for the <see cref="Category"/> aggregate.
/// </summary>
/// <remarks>
/// Both lookups below run through the <c>DbContext</c>, so both are already scoped to the acting tenant
/// by the query filter. That is what makes <see cref="ExistsByNameAsync"/> a per-tenant uniqueness check
/// and <see cref="IRepository{TEntity,TId}.FindByIdAsync"/> return <c>null</c> — rather than another
/// tenant's row — for an id that belongs to a different tenant.
/// </remarks>
public interface ICategoryRepository : IRepository<Category, Guid>
{
    /// <summary>
    /// Checks whether the acting tenant already has a category with this name, ignoring case and
    /// surrounding whitespace.
    /// </summary>
    /// <param name="name">The name as received; normalized before comparison.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when a category in this tenant already uses the name.</returns>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a page of the acting tenant's categories, optionally filtered by name.
    /// </summary>
    /// <remarks>
    /// Ordered by name and then by id: paging needs a total order or two rows with the same name could
    /// appear on both pages, or on neither.
    /// </remarks>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="filter">An optional case-insensitive substring to match against the name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of categories, the total count and the total number of pages.</returns>
    Task<(IEnumerable<Category> Items, long TotalCount, long TotalPages)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? filter,
        CancellationToken cancellationToken = default);
}
