using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Persistence;

namespace ShopIt.Notifications.Persistence.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The Notifications service only consumes integration events, so its schema
        // consists solely of the Kafka inbox/outbox tables (idempotent delivery).
        modelBuilder.ApplyInboxOutboxConfigurations();
    }
}
