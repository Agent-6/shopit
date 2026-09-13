using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Configurations;

/// <summary>
/// Configures <see cref="PermissionCatalogEntry"/> — a permission definition persisted in
/// the Identity service's catalog. The catalog is system-wide (not tenant-scoped), and a
/// permission name is unique across the whole catalog regardless of which service it came
/// from, so grants resolve unambiguously to a single definition.
/// </summary>
public class PermissionCatalogEntryConfiguration : IEntityTypeConfiguration<PermissionCatalogEntry>
{
    public void Configure(EntityTypeBuilder<PermissionCatalogEntry> builder)
    {
        builder.ToTable("PermissionCatalogEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.GroupName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.GroupDisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Description)
            .HasMaxLength(1024);

        builder.Property(e => e.SourceService)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_PermissionCatalogEntries_Name");

        builder.HasIndex(e => e.GroupName);
    }
}
