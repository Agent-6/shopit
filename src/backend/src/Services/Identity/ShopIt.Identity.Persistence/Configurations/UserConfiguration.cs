using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Configurations;

/// <summary>
/// Configures <see cref="User"/> so that user-name uniqueness and email lookups are
/// scoped to the tenant.
/// </summary>
/// <remarks>
/// ASP.NET Core Identity's default model defines a <em>global</em> unique index on
/// <see cref="User.NormalizedUserName"/> ("UserNameIndex"), which prevents the same
/// user name from being used in different tenants. This configuration replaces it with
/// a composite unique index on (NormalizedUserName, TenantId), allowing each tenant to
/// have its own user namespace while keeping user names unique within a tenant. The
/// email index is scoped the same way so lookups filter by tenant first.
/// </remarks>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Remove the Identity default global unique index on NormalizedUserName.
        // Indexes are matched by their properties because their names are only
        // materialized from the HasDatabaseName annotation during model finalization.
        var userNameIndex = builder.Metadata.GetIndexes()
            .FirstOrDefault(i => i.IsUnique
                && i.Properties.Count == 1
                && i.Properties[0].Name == nameof(User.NormalizedUserName));
        if (userNameIndex is not null)
        {
            builder.Metadata.RemoveIndex(userNameIndex);
        }

        // Remove the Identity default global index on NormalizedEmail.
        var emailIndex = builder.Metadata.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(User.NormalizedEmail));
        if (emailIndex is not null)
        {
            builder.Metadata.RemoveIndex(emailIndex);
        }

        // User-name uniqueness is scoped per tenant.
        builder.HasIndex(u => new { u.NormalizedUserName, u.TenantId })
            .IsUnique()
            .HasDatabaseName("UserNameIndex");

        // Email lookups are scoped per tenant. Uniqueness is still enforced at the
        // application level (RequireUniqueEmail + the tenant query filter), matching
        // the existing non-unique index semantics.
        builder.HasIndex(u => new { u.NormalizedEmail, u.TenantId })
            .HasDatabaseName("EmailIndex");
    }
}
