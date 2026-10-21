using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Repositories;

namespace ShopIt.Catalog.Domain.Repositories;

/// <summary>
/// The repository contract for the <see cref="Product"/> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// Every method runs through the <c>DbContext</c>, so all of them are already scoped to the acting tenant by
/// the query filter: <see cref="ExistsBySkuAsync"/> is a per-tenant uniqueness check, the paged read only
/// sees this tenant's rows, and <see cref="IRepository{TEntity,TId}.FindByIdAsync"/> returns <c>null</c> —
/// rather than another tenant's row — for an id that belongs to a different tenant.
/// </para>
/// </remarks>
public interface IProductRepository : IRepository<Product, Guid>
{
    /// <summary>
    /// Checks whether the acting tenant already has a product with this SKU, ignoring case and surrounding
    /// whitespace.
    /// </summary>
    /// <param name="sku">The SKU as received; normalized before comparison.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when a product in this tenant already uses the SKU.</returns>
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any product in the acting tenant references the given category.
    /// </summary>
    /// <remarks>
    /// This is the guard behind <c>DELETE /categories/{id}</c>: deleting a category that products still point
    /// at would either orphan them or, with a cascading delete, silently remove the products. The foreign key
    /// is restrict-on-delete as a backstop, and this check turns the failure into a 409 a client can act on.
    /// </remarks>
    /// <param name="categoryId">The category to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when at least one product in this tenant uses the category.</returns>
    Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a page of the acting tenant's products, optionally filtered by a name/SKU term, a category and a
    /// status.
    /// </summary>
    /// <remarks>
    /// The sort is a total order: <paramref name="sortBy"/> is followed by <c>Id</c>, so two products that
    /// share a name, price or creation instant cannot appear on both of two pages — or, worse, on neither.
    /// </remarks>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="filter">An optional case-insensitive term matched against the name or the SKU.</param>
    /// <param name="categoryId">An optional category to filter by.</param>
    /// <param name="status">An optional lifecycle status to filter by.</param>
    /// <param name="sortBy">The column to order by.</param>
    /// <param name="sortOrder">The direction of that order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of products, the total count and the total number of pages.</returns>
    Task<(IEnumerable<Product> Items, long TotalCount, long TotalPages)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? filter,
        Guid? categoryId,
        ProductStatus? status,
        ProductSortField sortBy,
        ProductSortOrder sortOrder,
        CancellationToken cancellationToken = default);
}
