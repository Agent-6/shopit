using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Persistence.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenant currentTenant) : IdentityDbContext<
    User,
    Role,
    Guid,
    UserClaim,
    UserRole,
    UserLogin,
    RoleClaim,
    UserToken
>(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ApplyTenantConfiguration(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    private void ApplyTenantConfiguration(ModelBuilder builder)
    {
        var applyTenantFilterMethod = typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                applyTenantFilterMethod?.MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [builder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>()
            .HasIndex(e => e.TenantId);

        builder.Entity<TEntity>()
            .HasQueryFilter(e => _currentTenant.Id == Guid.Empty || e.TenantId == _currentTenant.Id);
    }
}
