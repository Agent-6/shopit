using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

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
        builder.UseOpenIddict<Guid>();
    }
}
