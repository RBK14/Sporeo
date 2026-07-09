using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Fixtures.Rules;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Fixtures;

/// <summary>
/// Represents a scheduled sporting event with lifecycle status, external provider linkage, and optional venue assignment.
/// </summary>
public sealed class Fixture : AggregateRoot<FixtureId>, IAuditable, IDeletable
{
    /// <summary>
    /// Gets the identifier of the venue where the fixture takes place, if assigned.
    /// </summary>
    public VenueId? VenueId { get; private set; }

    /// <summary>
    /// Gets the identifier of the sport to which the fixture belongs.
    /// </summary>
    public SportId SportId { get; private set; }

    /// <summary>
    /// Gets the identifier of the league to which the fixture belongs, if specified.
    /// </summary>
    public LeagueId? LeagueId { get; private set; }

    /// <summary>
    /// Gets the identifier of the season to which the fixture belongs, if specified.
    /// </summary>
    public SeasonId? SeasonId { get; private set; }

    /// <summary>
    /// Gets the display name of the fixture.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the scheduled start date and time of the fixture.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// Gets the current lifecycle status of the fixture.
    /// </summary>
    public FixtureStatus Status { get; private set; }

    /// <summary>
    /// Gets the name of the external data provider, if the fixture originated from synchronization.
    /// </summary>
    public string? ExternalProviderName { get; private set; }

    /// <summary>
    /// Gets the identifier assigned by the external data provider, if the fixture originated from synchronization.
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the fixture has been manually edited and is locked from external synchronization.
    /// </summary>
    public bool IsManuallyEdited { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOn { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOn { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedOn { get; private set; }

    private Fixture(
        FixtureId id,
        SportId sportId,
        LeagueId? leagueId,
        SeasonId? seasonId,
        string name,
        DateTimeOffset startDate,
        string? externalProviderName,
        string? externalProviderId,
        bool isManuallyEdited) : base(id)
    {
        SportId = sportId;
        LeagueId = leagueId;
        SeasonId = seasonId;
        Name = name;
        StartDate = startDate;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        Status = FixtureStatus.Scheduled;
        IsManuallyEdited = isManuallyEdited;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a fixture from external provider data.
    /// </summary>
    /// <param name="sportId">The sport to which the fixture belongs.</param>
    /// <param name="leagueId">The league to which the fixture belongs, if known.</param>
    /// <param name="seasonId">The season to which the fixture belongs, if known.</param>
    /// <param name="name">The display name of the fixture.</param>
    /// <param name="startDate">The scheduled start date and time.</param>
    /// <param name="providerName">The name of the external data provider.</param>
    /// <param name="providerId">The identifier assigned by the external data provider.</param>
    /// <returns>A successful result containing the new fixture, or a failure when validation fails.</returns>
    public static Result<Fixture> CreateFromProvider(
        SportId sportId,
        LeagueId? leagueId,
        SeasonId? seasonId,
        string name,
        DateTimeOffset startDate,
        string providerName,
        string providerId)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Fixture>(nameValidation.Error);

        var providerValidation = ValidateProvider(providerName, providerId);
        if (providerValidation.IsFailure)
            return Result.Failure<Fixture>(providerValidation.Error);

        return new Fixture(
            FixtureId.New(),
            sportId,
            leagueId,
            seasonId,
            name,
            startDate,
            providerName,
            providerId,
            false);
    }

    /// <summary>
    /// Creates a fixture entered manually without external provider linkage.
    /// </summary>
    /// <param name="sportId">The sport to which the fixture belongs.</param>
    /// <param name="leagueId">The league to which the fixture belongs, if known.</param>
    /// <param name="seasonId">The season to which the fixture belongs, if known.</param>
    /// <param name="name">The display name of the fixture.</param>
    /// <param name="startDate">The scheduled start date and time.</param>
    /// <returns>A successful result containing the new fixture, or a failure when validation fails.</returns>
    public static Result<Fixture> CreateManually(
        SportId sportId,
        LeagueId? leagueId,
        SeasonId? seasonId,
        string name,
        DateTimeOffset startDate)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Fixture>(nameValidation.Error);

        return new Fixture(
            FixtureId.New(),
            sportId,
            leagueId,
            seasonId,
            name,
            startDate,
            null,
            null,
            true);
    }

    /// <summary>
    /// Assigns a venue to the fixture.
    /// </summary>
    /// <param name="venueId">The identifier of the venue to assign.</param>
    /// <returns>A successful result when the venue is assigned; otherwise, a failure when the fixture cannot be modified.</returns>
    public Result AssignVenue(VenueId venueId)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        VenueId = venueId;

        return Result.Success();
    }

    /// <summary>
    /// Updates the fixture with data received from an external provider.
    /// </summary>
    /// <param name="sportId">The sport to which the fixture belongs.</param>
    /// <param name="leagueId">The league to which the fixture belongs, if known.</param>
    /// <param name="seasonId">The season to which the fixture belongs, if known.</param>
    /// <param name="name">The display name of the fixture.</param>
    /// <param name="startDate">The scheduled start date and time.</param>
    /// <returns>A successful result when synchronization succeeds; otherwise, a failure when the fixture is locked, deleted, finished, or validation fails.</returns>
    public Result SyncExternalData(
        SportId sportId,
        LeagueId? leagueId,
        SeasonId? seasonId,
        string name,
        DateTimeOffset startDate)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var syncGuard = CheckRule(new ManuallyEditedFixtureCannotBeSyncedRule(this));
        if (syncGuard.IsFailure)
            return syncGuard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(sportId, leagueId, name, startDate);

        return Result.Success();
    }

    /// <summary>
    /// Updates the fixture with manually entered data and locks it from external synchronization.
    /// </summary>
    /// <param name="sportId">The sport to which the fixture belongs.</param>
    /// <param name="leagueId">The league to which the fixture belongs, if known.</param>
    /// <param name="name">The display name of the fixture.</param>
    /// <param name="startDate">The scheduled start date and time.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the fixture cannot be modified or validation fails.</returns>
    public Result UpdateManually(
        SportId sportId,
        LeagueId? leagueId,
        string name,
        DateTimeOffset startDate)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(sportId, leagueId, name, startDate);
        IsManuallyEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Transitions the fixture to a new lifecycle status.
    /// </summary>
    /// <param name="newStatus">The requested status.</param>
    /// <returns>A successful result when the transition is allowed; otherwise, a failure when the transition is invalid or the fixture cannot be modified.</returns>
    public Result ChangeStatus(FixtureStatus newStatus)
    {
        if (Status == newStatus)
            return Result.Success();

        var deletedGuard = EnsureNotDeleted();
        if (deletedGuard.IsFailure)
            return deletedGuard;

        var guard = CheckRules(
            new FinishedFixtureIsImmutableRule(this),
            new InvalidFixtureStatusTransitionRule(Status, newStatus));
        if (guard.IsFailure)
            return guard;

        Status = newStatus;

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the fixture.
    /// </summary>
    /// <returns>A successful result. Idempotent when the fixture is already deleted.</returns>
    public Result Delete()
    {
        if (IsDeleted)
            return Result.Success();

        IsDeleted = true;

        return Result.Success();
    }

    private Result EnsureModifiable()
    {
        var deletedGuard = EnsureNotDeleted();
        if (deletedGuard.IsFailure)
            return deletedGuard;

        return CheckRule(new FinishedFixtureIsImmutableRule(this));
    }

    private Result EnsureNotDeleted() =>
        IsDeleted
            ? Result.Failure(Errors.Fixture.Deleted)
            : Result.Success();

    private static Result ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result.Failure(Errors.Fixture.EmptyName)
            : Result.Success();

    private static Result ValidateProvider(string providerName, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return Result.Failure(Errors.Fixture.EmptyProviderName);

        if (string.IsNullOrWhiteSpace(providerId))
            return Result.Failure(Errors.Fixture.EmptyProviderId);

        return Result.Success();
    }

    private void UpdateCoreFields(
        SportId sportId,
        LeagueId? leagueId,
        string name,
        DateTimeOffset startDate)
    {
        SportId = sportId;
        LeagueId = leagueId;
        Name = name;
        StartDate = startDate;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Fixture"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Fixture() { }
#pragma warning restore CS8618
}
