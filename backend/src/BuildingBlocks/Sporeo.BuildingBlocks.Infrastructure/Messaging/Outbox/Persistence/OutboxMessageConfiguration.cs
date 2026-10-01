using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;

/// <summary>
/// EF Core mapping for durable outbox messages.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox-messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.Status, x.NextAttempt, x.OccurredOn })
            .HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
