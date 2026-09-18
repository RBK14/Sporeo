using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Domain.Seasons;

/// <summary>
/// Represents a competitive season within a league.
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
        string? externalProviderName,
        string? externalProviderId) : base(id)
    {
        LeagueId = leagueId;
        Name = name;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        IsCurrent = false;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a new season.
    /// </summary>
    /// <param name="leagueId">The league to which the season belongs.</param>
    /// <param name="name">The display name of the season.</param>
    /// <param name="externalProviderName">The name of the external data provider, if known.</param>
    /// <param name="externalProviderId">The identifier assigned by the external data provider, if known.</param>
    /// <returns>A successful result containing the new season, or a failure when validation fails.</returns>
    public static Result<Season> Create(
        LeagueId leagueId,
        string name,
        string? externalProviderName = null,
        string? externalProviderId = null)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Season>(nameValidation.Error);

        return new Season(
            SeasonId.New(),
            leagueId,
            name,
            externalProviderName,
            externalProviderId);
    }

    /// <summary>
    /// Updates the display name of the season.
    /// </summary>
    /// <param name="name">The new display name.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the season cannot be modified or validation fails.</returns>
    public Result Update(string name)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        Name = name;
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

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Season"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Season() { }
#pragma warning restore CS8618
}