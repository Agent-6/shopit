using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopIt.Notifications.Persistence.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=notifications-db;Username=postgres;Password=postgres");

        return new NotificationsDbContext(optionsBuilder.Options);
    }
}
