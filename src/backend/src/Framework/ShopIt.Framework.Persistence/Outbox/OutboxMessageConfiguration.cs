using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ShopIt.Framework.Persistence.Outbox;

/// <summary>
/// EF Core fluent configuration for the <see cref="OutboxMessage"/> entity.
/// Call <c>builder.ApplyConfiguration(new OutboxMessageConfiguration())</c>
/// in the service's <c>OnModelCreating</c>, or use the helper extension on the model builder.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.OccurredOn)
            .IsRequired();

        builder.Property(m => m.ProcessedOn);

        builder.Property(m => m.Error)
            .HasMaxLength(2048);

        // Index for efficient polling by the OutboxProcessor: unprocessed messages ordered by occurrence
        builder.HasIndex(m => new { m.ProcessedOn, m.OccurredOn })
            .HasDatabaseName("IX_OutboxMessages_ProcessedOn_OccurredOn");
    }
}
