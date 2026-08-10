using Microsoft.EntityFrameworkCore;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Persistence;

namespace ShopIt.Tenancy.Persistence.Data;

public class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore domain events so EF Core doesn't try to map DomainEvent as an entity
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.ApplyInboxOutboxConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
    }
}
