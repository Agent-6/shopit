using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Configurations;

/// <summary>
/// Configures <see cref="RoleClaim"/> — permissions are stored as claims on roles.
/// </summary>
/// <remarks>
/// Adds a unique index on (RoleId, ClaimType, ClaimValue) so a permission can be
/// granted to a role at most once. Because every role belongs to exactly one tenant
/// (and role IDs are unique across tenants), this index is inherently tenant-scoped:
/// the same permission may be granted to same-named roles in different tenants without
/// colliding, while duplicates within a single role are rejected at the database level.
/// </remarks>
public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(rc => new { rc.RoleId, rc.ClaimType, rc.ClaimValue })
            .IsUnique();
    }
}
