using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ShopIt.Identity.Persistence.Data;

internal class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext(options)
{
}
