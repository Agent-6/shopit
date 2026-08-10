using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;

namespace ShopIt.Framework.Persistence;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Apply InboxMessage and OutboxMessage Configurations.
    /// </summary>
    public static ModelBuilder ApplyInboxOutboxConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        return modelBuilder;
    }
}
