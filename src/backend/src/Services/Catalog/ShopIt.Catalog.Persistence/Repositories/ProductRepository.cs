using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Catalog.Persistence.Data;
using ShopIt.Framework.Persistence.Repositories;

namespace ShopIt.Catalog.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProductRepository"/>.
/// </summary>
/// <remarks>
/// Every query goes through <c>DbSet</c>, so the tenant query filter is always in play: the SKU check is per
/// tenant, the category guard only sees this tenant's products, and a paged read can only return rows of the
/// acting tenant.
/// </remarks>
public class ProductRepository(CatalogDbContext dbContext)
    : Repository<Product, Guid, CatalogDbContext>(dbContext), IProductRepository
{
    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalizedSku = Product.NormalizeSku(sku);

        return await DbSet.AnyAsync(
            product => product.NormalizedSku == normalizedSku,
            cancellationToken);
    }

    public async Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(product => product.CategoryId == categoryId, cancellationToken);

    public async Task<(IEnumerable<Product> Items, long TotalCount, long TotalPages)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? filter,
        Guid? categoryId,
        ProductStatus? status,
        ProductSortField sortBy,
        ProductSortOrder sortOrder,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            // The same term against both columns, ILIKE so casing does not matter: a shopkeeper searching a
            // catalog types "tee", not "Classic Tee", and a storefront's search box behaves the same way.
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, $"%{filter}%")
                || EF.Functions.ILike(product.Sku, $"%{filter}%"));
        }

        if (categoryId is { } category)
        {
            query = query.Where(product => product.CategoryId == category);
        }

        if (status is { } productStatus)
        {
            query = query.Where(product => product.Status == productStatus);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = (totalCount / pageSize) + (totalCount % pageSize > 0 ? 1 : 0);

        var products = await Order(query, sortBy, sortOrder)
            // The tie-breaker is not decorative: names and prices repeat, and a non-total order lets
            // PostgreSQL return the same row on two pages while skipping another.
            .ThenBy(product => product.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount, totalPages);
    }

    /// <summary>
    /// Applies the requested ordering, before the <c>Id</c> tie-breaker the caller adds.
    /// </summary>
    /// <remarks>
    /// A <c>switch</c> over the enum rather than a string: it is exhaustive, so adding a sortable field without
    /// teaching this method about it is a compile error rather than a silent fall-back. SKU is sorted through
    /// its normalized form because a shop reads <c>TEE-001</c> next to <c>TEE-002</c>, not grouped by casing.
    /// </remarks>
    private static IOrderedQueryable<Product> Order(
        IQueryable<Product> query,
        ProductSortField sortBy,
        ProductSortOrder sortOrder) =>
        sortBy switch
        {
            ProductSortField.Name => Apply(query, product => product.Name, sortOrder),
            ProductSortField.Sku => Apply(query, product => product.NormalizedSku, sortOrder),
            ProductSortField.Price => Apply(query, product => product.Price.Amount, sortOrder),
            ProductSortField.CreatedOn => Apply(query, product => product.CreatedOn, sortOrder),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sortBy),
                sortBy,
                "Unknown product sort field.")
        };

    private static IOrderedQueryable<Product> Apply<TKey>(
        IQueryable<Product> query,
        Expression<Func<Product, TKey>> keySelector,
        ProductSortOrder sortOrder) =>
        sortOrder is ProductSortOrder.Descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
}
