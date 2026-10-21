using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Persistence.Auditing;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// Seeds the demo tenant's categories and products so the portal has a catalog to show on a fresh database.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately idempotent per row rather than "only when the tables are empty": the check is the same shape
/// as Identity's seeding, and it means a partially seeded database is completed instead of skipped.
/// </para>
/// <para>
/// It seeds into the demo tenant explicitly and never into host scope — at host scope the tenant filter is
/// disabled and a row would be stamped with no owner, which is the failure ADR-0001 warns about. The tenant
/// is entered with <see cref="ICurrentTenant.Change"/>, the only supported way to change scope.
/// </para>
/// <para>
/// Audit fields are stamped by <see cref="AuditInfo.Apply"/> with no actor: seeding has no interactive user,
/// and ADR-0004's <c>null</c> means exactly that rather than a sentinel id.
/// </para>
/// </remarks>
public class CatalogDataSeeder(
    CatalogDbContext dbContext,
    ICurrentTenant currentTenant,
    ILogger<CatalogDataSeeder> logger)
{
    /// <summary>The tenant Identity seeds its demo users into; the catalog's sample data lives here too.</summary>
    public static readonly Guid SeededTenantId = new("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E");

    /// <summary>A display name for <see cref="SeededTenantId"/>, for logs and the ambient tenant.</summary>
    public const string SeededTenantName = "Seeded Tenant";

    private const string ApparelCategory = "Apparel";
    private const string AccessoriesCategory = "Accessories";

    /// <summary>
    /// The currency every seeded price is quoted in.
    /// </summary>
    /// <remarks>
    /// A constant rather than the demo tenant's currency read from the registry, on purpose: a seeded price is a
    /// historical fact like any other, and startup must not depend on another service being up to insert sample data.
    /// It is <c>EUR</c> because that is the currency every tenant was migrated to (Tenant.DefaultCurrency, ADR-0006),
    /// so the demo catalogue agrees with the tenant it belongs to on a fresh database. If the demo tenant's currency
    /// is later changed, these three products keep theirs and the product detail read reports both.
    /// </remarks>
    public const string SeedCurrency = "EUR";

    private readonly CatalogDbContext _dbContext = dbContext;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ILogger<CatalogDataSeeder> _logger = logger;

    /// <summary>
    /// Ensures the demo tenant has two categories and three products, priced in <see cref="SeedCurrency"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var tenantChange = _currentTenant.Change(new TenantInfo(SeededTenantId, SeededTenantName));

        // Two reads rather than one per row: seeding runs at startup, but it should not issue a query per
        // sample row to decide it has nothing to do.
        var existingCategories = await _dbContext.Categories.ToListAsync(cancellationToken);
        var existingSkus = await _dbContext.Products
            .Select(product => product.NormalizedSku)
            .ToListAsync(cancellationToken);

        var categories = new Dictionary<string, Category>(StringComparer.Ordinal);
        foreach (var name in new[] { ApparelCategory, AccessoriesCategory })
        {
            var normalizedName = Category.NormalizeName(name);

            var category = existingCategories.FirstOrDefault(
                candidate => string.Equals(candidate.NormalizedName, normalizedName, StringComparison.Ordinal));

            if (category is null)
            {
                category = Category.Create(Guid.NewGuid(), SeededTenantId, name);
                _dbContext.Categories.Add(category);
                _logger.LogInformation("Seeding category {Category} for tenant {TenantId}", name, SeededTenantId);
            }

            categories[name] = category;
        }

        var samples = new[]
        {
            new SeedProduct("Classic Tee", "TEE-001", "Heavyweight cotton t-shirt, unisex.", 19.99m, ApparelCategory, ProductStatus.Active),
            new SeedProduct("Canvas Tote", "TOTE-001", "Reusable canvas shopping bag.", 24.50m, AccessoriesCategory, ProductStatus.Active),
            new SeedProduct("Wool Beanie", "BEANIE-001", null, 14.00m, AccessoriesCategory, ProductStatus.Draft),
        };

        foreach (var sample in samples)
        {
            if (existingSkus.Contains(Product.NormalizeSku(sample.Sku), StringComparer.Ordinal))
            {
                continue;
            }

            var product = Product.Create(
                Guid.NewGuid(),
                SeededTenantId,
                sample.Name,
                sample.Sku,
                sample.Description,
                Money.Create(sample.Amount, SeedCurrency),
                categories[sample.Category].Id);

            // Created Draft and then moved, so the seeded data cannot claim a transition the aggregate would
            // refuse — the same lifecycle the endpoints enforce.
            if (sample.Status is not ProductStatus.Draft)
            {
                product.ChangeStatus(sample.Status);
            }

            _dbContext.Products.Add(product);
            _logger.LogInformation("Seeding product {Sku} for tenant {TenantId}", sample.Sku, SeededTenantId);
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            AuditInfo.Apply(_dbContext, actorId: null);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>One sample product: what to insert, and the state to insert it in.</summary>
    private sealed record SeedProduct(
        string Name,
        string Sku,
        string? Description,
        decimal Amount,
        string Category,
        ProductStatus Status);
}
