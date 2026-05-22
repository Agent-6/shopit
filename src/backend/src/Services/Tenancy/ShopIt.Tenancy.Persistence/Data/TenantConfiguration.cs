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

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.Property(t => t.IsActive)
            .IsRequired();

        // Audit properties
        builder.Property(t => t.CreatedOn)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .IsRequired();

        builder.Property(t => t.LastModifiedOn);
        builder.Property(t => t.LastModifiedBy);
        builder.Property(t => t.DeletedOn);
        builder.Property(t => t.DeletedBy);
    }
}
