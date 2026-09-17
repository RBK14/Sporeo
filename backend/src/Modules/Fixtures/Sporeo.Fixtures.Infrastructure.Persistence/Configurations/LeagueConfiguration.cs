using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="League"/>.
/// </summary>
internal class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("leagues");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => LeagueId.FromValue(value))
            .ValueGeneratedNever();

        builder.Property(x => x.SportId)
            .HasConversion(
                id => id.Value,
                value => SportId.FromValue(value))
            .IsRequired();

        builder.HasOne<Sport>()
            .WithMany()
            .HasForeignKey(x => x.SportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Country)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderName)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderId)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExternalProviderName, x.ExternalProviderId })
            .IsUnique()
            .HasFilter("[ExternalProviderName] IS NOT NULL AND [ExternalProviderId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_leagues_ExternalProvider");

        builder.Property(x => x.IsMonitored)
            .IsRequired();

        builder.HasIndex(x => x.IsMonitored)
            .HasDatabaseName("IX_Leagues_ActiveMonitored")
            .HasFilter("[IsMonitored] = 1 AND [IsDeleted] = 0");

        builder.Property(x => x.IsManuallyEdited)
            .IsRequired();

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOn);
    }
}
