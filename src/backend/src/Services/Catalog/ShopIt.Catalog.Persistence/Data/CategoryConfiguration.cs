using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Catalog.Domain.Categories;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// Maps the <see cref="Category"/> aggregate.
/// </summary>
/// <remarks>
/// The tenant query filter and the plain <c>TenantId</c> index are not here: they are applied by
/// <c>CatalogDbContext.ApplyTenantFilters</c> to every <see cref="ShopIt.Framework.Domain.Tenancy.ITenantEntity"/>,
/// which is the single place tenant isolation is configured (ADR-0001). What is here is the
/// tenant-scoped uniqueness, which is this aggregate's own rule. That leaves two indexes leading with
/// <c>TenantId</c> — the framework's single-column one and this composite one — which is deliberate
/// waste rather than an oversight: the framework index belongs to the isolation guard and is not this
/// aggregate's to remove.
/// </remarks>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(Category.MaxNameLength);

        builder.Property(c => c.NormalizedName)
            .IsRequired()
            .HasMaxLength(Category.MaxNameLength);

        // Uniqueness is per tenant and case-insensitive: the index leads with TenantId (so two tenants
        // may each own a "Tools" category) and compares the normalized name, because a PostgreSQL unique
        // index is case-sensitive and EF Core cannot express a functional index on lower(Name) in the
        // model. See Category.NormalizedName for why it is a column rather than an index expression.
        builder.HasIndex(c => new { c.TenantId, c.NormalizedName })
            .IsUnique();

        // Audit properties. Values are written by the persistence convention (ADR-0004); only the
        // creation instant is required, because every row has one.
        builder.Property(c => c.CreatedOn)
            .IsRequired();

        builder.Property(c => c.CreatedBy);
        builder.Property(c => c.LastModifiedOn);
        builder.Property(c => c.LastModifiedBy);
        builder.Property(c => c.DeletedOn);
        builder.Property(c => c.DeletedBy);
    }
}
