using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Persistence.Data;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Three characters, upper case, no database default: the aggregate always has a value (EUR unless it was
        // changed), so the column has nothing to default. The migration that added it supplied 'EUR' for the rows
        // that already existed, which is the same value the aggregate would have chosen — see ADR-0006 for why
        // that is the answer that changes no price's meaning.
        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(Tenant.CurrencyCodeLength);

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.Property(t => t.IsActive)
            .IsRequired();

        // Audit properties
        builder.Property(t => t.CreatedOn)
            .IsRequired();


        builder.Property(t => t.LastModifiedOn);
        builder.Property(t => t.LastModifiedBy);
        builder.Property(t => t.DeletedOn);
        builder.Property(t => t.DeletedBy);
    }
}
