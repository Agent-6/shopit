using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Repositories;

namespace ShopIt.Catalog.Domain.Repositories;

/// <summary>
/// The repository contract for the <see cref="Product"/> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// Both lookups below run through the <c>DbContext</c>, so both are already scoped to the acting tenant by
/// the query filter: <see cref="ExistsBySkuAsync"/> is a per-tenant uniqueness check and
/// <see cref="IRepository{TEntity,TId}.FindByIdAsync"/> returns <c>null</c> — rather than another tenant's
/// row — for an id that belongs to a different tenant.
/// </para>
/// <para>
/// There are no listing or search methods here: the product list and detail query surface is #79, and adding
/// a paged read now would be scaffolding that issue has to keep in step.
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
}
