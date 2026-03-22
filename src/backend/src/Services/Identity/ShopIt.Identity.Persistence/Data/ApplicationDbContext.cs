using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<
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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
