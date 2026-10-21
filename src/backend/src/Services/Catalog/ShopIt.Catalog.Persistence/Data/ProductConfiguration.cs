using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// Maps the <see cref="Product"/> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// The tenant query filter and the plain <c>TenantId</c> index are not here: <c>CatalogDbContext</c>'s
/// <c>ApplyTenantFilters</c> applies them to every <c>ITenantEntity</c>, which is the single place tenant
/// isolation is configured (ADR-0001). What is here is this aggregate's own rules — SKU uniqueness per
/// tenant, the price's column, the status's representation and the category reference.
/// </para>
/// <para>
/// The price is stored as two columns — <c>Price</c> as <c>numeric(18,2)</c> and <c>PriceCurrency</c> — rather
/// than as one amount whose currency the reader re-derives from configuration. ADR-0005 has the currency
/// coming from the tenant and not stored per product, but there is no tenant currency to read yet (#84), so
/// the value the price was stamped with is persisted: otherwise changing <c>Catalog:Currency</c> would
/// silently re-label every historical price, which is a financial bug with no symptom in the code.
/// </para>
/// </remarks>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.TenantId)
            .IsRequired();

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Product.MaxNameLength);

        builder.Property(product => product.Sku)
            .IsRequired()
            .HasMaxLength(Product.MaxSkuLength);

        builder.Property(product => product.NormalizedSku)
            .IsRequired()
            .HasMaxLength(Product.MaxSkuLength);

        builder.Property(product => product.Description)
            .HasMaxLength(Product.MaxDescriptionLength);

        // Uniqueness is per tenant and case-insensitive, the same shape as a category's name: the index
        // leads with TenantId (two tenants may each sell TEE-001) and compares the normalized SKU, because a
        // PostgreSQL unique index is case-sensitive and EF Core cannot express a functional index on
        // upper(Sku) in the model. See Product.NormalizedSku.
        builder.HasIndex(product => new { product.TenantId, product.NormalizedSku })
            .IsUnique();

        // Stored by name, not by number: a status is a lifecycle fact, and a database whose rows say "1"
        // cannot be read without this enum. The length covers the longest name with room to add one.
        builder.Property(product => product.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        // Complex type rather than an owned entity: Money has no identity of its own, so it is part of the
        // Products table, which is what "two columns" means here.
        builder.ComplexProperty(product => product.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(money => money.CurrencyCode)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(Money.CurrencyCodeLength)
                .IsRequired();
        });

        // Optional reference to another aggregate: nullable, no navigation, and restrict-on-delete so a
        // category with products cannot be removed by a cascading delete (the guard in
        // DeleteCategoryCommandHandler turns the same rule into a 409 before this constraint is reached).
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit properties. Values are written by the persistence convention (ADR-0004); only the creation
        // instant is required, because every row has one.
        builder.Property(product => product.CreatedOn)
            .IsRequired();

        builder.Property(product => product.CreatedBy);
        builder.Property(product => product.LastModifiedOn);
        builder.Property(product => product.LastModifiedBy);
        builder.Property(product => product.DeletedOn);
        builder.Property(product => product.DeletedBy);
    }
}
