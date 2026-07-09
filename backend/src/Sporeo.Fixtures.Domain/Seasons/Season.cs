using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.Rules;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Domain.Seasons;

/// <summary>
/// Represents a competitive season within a league, with a date range and optional current-season flag.
/// </summary>
public sealed class Season : AggregateRoot<SeasonId>, IAuditable, IDeletable
{
    /// <summary>
    /// Gets the identifier of the league to which the season belongs.
    /// </summary>
    public LeagueId LeagueId { get; private set; }

    /// <summary>
    /// Gets the display name of the season.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the start date of the season.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// Gets the end date of the season.
    /// </summary>
    public DateTimeOffset EndDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this season is marked as the current season for its league.
    /// </summary>
    public bool IsCurrent { get; private set; }

    /// <summary>
    /// Gets the name of the external data provider, if the season originated from synchronization.
    /// </summary>
    public string? ExternalProviderName { get; private set; }

    /// <summary>
    /// Gets the identifier assigned by the external data provider, if the season originated from synchronization.
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the season has been manually edited and is locked from external synchronization.
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

    private Season(
        SeasonId id,
        LeagueId leagueId,
        string name,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? externalProviderName,
        string? externalProviderId,
        bool isManuallyEdited) : base(id)
    {
        LeagueId = leagueId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        IsManuallyEdited = isManuallyEdited;
        IsCurrent = false;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a season from external provider data.
    /// </summary>
    /// <param name="leagueId">The league to which the season belongs.</param>
    /// <param name="name">The display name of the season.</param>
    /// <param name="startDate">The start date of the season.</param>
    /// <param name="endDate">The end date of the season. Must be after <paramref name="startDate"/>.</param>
    /// <param name="providerName">The name of the external data provider.</param>
    /// <param name="providerId">The identifier assigned by the external data provider.</param>
    /// <returns>A successful result containing the new season, or a failure when validation fails.</returns>
    public static Result<Season> CreateFromProvider(
        LeagueId leagueId,
        string name,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string providerName,
        string providerId)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Season>(nameValidation.Error);

        var providerValidation = ValidateProvider(providerName, providerId);
        if (providerValidation.IsFailure)
            return Result.Failure<Season>(providerValidation.Error);

        var datesValidation = ValidateDates(startDate, endDate);
        if (datesValidation.IsFailure)
            return Result.Failure<Season>(datesValidation.Error);

        return new Season(
            SeasonId.New(),
            leagueId,
            name,
            startDate,
            endDate,
            providerName,
            providerId,
            false);
    }

    /// <summary>
    /// Creates a season entered manually without external provider linkage.
    /// </summary>
    /// <param name="leagueId">The league to which the season belongs.</param>
    /// <param name="name">The display name of the season.</param>
    /// <param name="startDate">The start date of the season.</param>
    /// <param name="endDate">The end date of the season. Must be after <paramref name="startDate"/>.</param>
    /// <returns>A successful result containing the new season, or a failure when validation fails.</returns>
    public static Result<Season> CreateManually(
        LeagueId leagueId,
        string name,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Season>(nameValidation.Error);

        var datesValidation = ValidateDates(startDate, endDate);
        if (datesValidation.IsFailure)
            return Result.Failure<Season>(datesValidation.Error);

        return new Season(
            SeasonId.New(),
            leagueId,
            name,
            startDate,
            endDate,
            null,
            null,
            true);
    }

    /// <summary>
    /// Updates the season with data received from an external provider.
    /// </summary>
    /// <param name="name">The display name of the season.</param>
    /// <param name="startDate">The start date of the season.</param>
    /// <param name="endDate">The end date of the season. Must be after <paramref name="startDate"/>.</param>
    /// <returns>A successful result when synchronization succeeds; otherwise, a failure when the season is locked, deleted, or validation fails.</returns>
    public Result SyncExternalData(string name, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var syncGuard = CheckRule(new ManuallyEditedSeasonCannotBeSyncedRule(this));
        if (syncGuard.IsFailure)
            return syncGuard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        var datesValidation = ValidateDates(startDate, endDate);
        if (datesValidation.IsFailure)
            return datesValidation;

        UpdateCoreFields(name, startDate, endDate);

        return Result.Success();
    }

    /// <summary>
    /// Updates the season with manually entered data and locks it from external synchronization.
    /// </summary>
    /// <param name="name">The display name of the season.</param>
    /// <param name="startDate">The start date of the season.</param>
    /// <param name="endDate">The end date of the season. Must be after <paramref name="startDate"/>.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the season cannot be modified or validation fails.</returns>
    public Result UpdateManually(string name, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        var datesValidation = ValidateDates(startDate, endDate);
        if (datesValidation.IsFailure)
            return datesValidation;

        UpdateCoreFields(name, startDate, endDate);
        IsManuallyEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Marks the season as the current season for its league.
    /// </summary>
    /// <returns>A successful result when the flag is set; otherwise, a failure when the season cannot be modified.</returns>
    public Result MarkAsCurrent()
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        IsCurrent = true;
        return Result.Success();
    }

    /// <summary>
    /// Clears the current-season flag from the season.
    /// </summary>
    /// <returns>A successful result when the flag is cleared; otherwise, a failure when the season cannot be modified.</returns>
    public Result UnmarkAsCurrent()
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        IsCurrent = false;
        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the season.
    /// </summary>
    /// <returns>A successful result. Idempotent when the season is already deleted.</returns>
    public Result Delete()
    {
        if (IsDeleted)
            return Result.Success();

        IsDeleted = true;
        return Result.Success();
    }

    private Result EnsureModifiable() =>
        EnsureNotDeleted();

    private Result EnsureNotDeleted() =>
        IsDeleted
            ? Result.Failure(Errors.Season.Deleted)
            : Result.Success();

    private static Result ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result.Failure(Errors.Season.EmptyName)
            : Result.Success();

    private static Result ValidateProvider(string providerName, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return Result.Failure(Errors.Season.EmptyProviderName);

        if (string.IsNullOrWhiteSpace(providerId))
            return Result.Failure(Errors.Season.EmptyProviderId);

        return Result.Success();
    }

    private static Result ValidateDates(DateTimeOffset startDate, DateTimeOffset endDate) =>
        startDate >= endDate
            ? Result.Failure(Errors.Season.InvalidDateRange)
            : Result.Success();

    private void UpdateCoreFields(string name, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Season"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Season() { }
#pragma warning restore CS8618
}
