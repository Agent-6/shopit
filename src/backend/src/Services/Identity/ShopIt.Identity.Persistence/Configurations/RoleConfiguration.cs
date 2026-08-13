using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Configurations;

/// <summary>
/// Configures <see cref="Role"/> so that role-name uniqueness is scoped to the tenant.
/// </summary>
/// <remarks>
/// ASP.NET Core Identity's default model defines a <em>global</em> unique index on
/// <see cref="Role.NormalizedName"/> ("RoleNameIndex"), which prevents the same role
/// name from existing in different tenants (e.g. a host-level "Admin" and a tenant-level
/// "Admin"). This configuration removes that index and replaces it with a composite
/// unique index on (NormalizedName, TenantId), allowing each tenant to have its own
/// role namespace while keeping names unique within a tenant.
/// </remarks>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Remove the Identity default global unique index on NormalizedName so it does
        // not remain alongside the new tenant-scoped index. The index is matched by its
        // properties because its name is only materialized from the HasDatabaseName
        // annotation during model finalization.
        var roleNameIndex = builder.Metadata.GetIndexes()
            .FirstOrDefault(i => i.IsUnique
                && i.Properties.Count == 1
                && i.Properties[0].Name == nameof(Role.NormalizedName));
        if (roleNameIndex is not null)
        {
            builder.Metadata.RemoveIndex(roleNameIndex);
        }

        // Role name uniqueness is scoped per tenant.
        builder.HasIndex(r => new { r.NormalizedName, r.TenantId })
            .IsUnique()
            .HasDatabaseName("RoleNameIndex");
    }
}
