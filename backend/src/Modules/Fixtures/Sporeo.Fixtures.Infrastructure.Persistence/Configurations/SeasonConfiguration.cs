using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Season"/>.
/// </summary>
internal class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => SeasonId.FromValue(value))
            .ValueGeneratedNever();

        builder.Property(x => x.LeagueId)
            .HasConversion(
                id => id.Value,
                value => LeagueId.FromValue(value))
            .IsRequired();

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.IsCurrent)
            .IsRequired();

        builder.Property(x => x.ExternalProviderName)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderId)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExternalProviderName, x.ExternalProviderId })
            .IsUnique()
            .HasFilter("[ExternalProviderName] IS NOT NULL AND [ExternalProviderId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_seasons_ExternalProvider");

        builder.Property(x => x.IsManuallyEdited)
            .IsRequired();

        builder.HasIndex(x => x.LeagueId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_seasons_LeagueId_UniqueCurrentSeason");

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOn);
    }
}
