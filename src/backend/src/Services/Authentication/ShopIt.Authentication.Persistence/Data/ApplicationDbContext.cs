using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Events;
using OpenIddict.EntityFrameworkCore.Models;
using ShopIt.Framework.Persistence;

namespace ShopIt.Authentication.Persistence.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<OpenIddictEntityFrameworkCoreApplication<Guid>> Applications { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization<Guid>> Authorizations { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreScope<Guid>> Scopes { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreToken<Guid>> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Ignore domain events so EF Core doesn't try to map DomainEvent as an entity
        builder.Ignore<DomainEvent>();

        builder.ApplyInboxOutboxConfigurations();

        builder.UseOpenIddict<Guid>();
    }
}
