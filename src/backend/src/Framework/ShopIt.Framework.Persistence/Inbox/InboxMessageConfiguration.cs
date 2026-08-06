using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// EF Core fluent configuration for the <see cref="InboxMessage"/> entity.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.ReceivedOn)
            .IsRequired();

        builder.Property(m => m.ProcessedOn);

        builder.Property(m => m.Error)
            .HasMaxLength(2048);

        // Index for efficient polling of unprocessed messages
        builder.HasIndex(m => new { m.ProcessedOn, m.ReceivedOn })
            .HasDatabaseName("IX_InboxMessages_ProcessedOn_ReceivedOn");
    }
}
