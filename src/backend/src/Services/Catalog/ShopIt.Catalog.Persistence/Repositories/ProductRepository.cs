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
/// tenant, and the category guard only sees this tenant's products.
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
}
