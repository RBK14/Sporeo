using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Sport"/>.
/// </summary>
internal class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("sports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => SportId.FromValue(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ExternalProviderName)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderId)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExternalProviderName, x.ExternalProviderId })
            .IsUnique()
            .HasFilter("[ExternalProviderName] IS NOT NULL AND [ExternalProviderId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_sports_ExternalProvider");

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOn);
    }
}
