using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.Rules;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Domain.Leagues;

/// <summary>
/// Represents a sporting league belonging to a sport, with optional external provider linkage.
/// </summary>
public sealed class League : AggregateRoot<LeagueId>, IAuditable, IDeletable
{
    /// <summary>
    /// Gets the identifier of the sport to which the league belongs.
    /// </summary>
    public SportId SportId { get; private set; }

    /// <summary>
    /// Gets the display name of the league.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the country in which the league operates, if specified.
    /// </summary>
    public string? Country { get; private set; }

    /// <summary>
    /// Gets the name of the external data provider, if the league originated from synchronization.
    /// </summary>
    public string? ExternalProviderName { get; private set; }

    /// <summary>
    /// Gets the identifier assigned by the external data provider, if the league originated from synchronization.
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the league has been manually edited and is locked from external synchronization.
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

    private League(
        LeagueId id,
        SportId sportId,
        string name,
        string? country,
        string? externalProviderName,
        string? externalProviderId,
        bool isManuallyEdited) : base(id)
    {
        SportId = sportId;
        Name = name;
        Country = country;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        IsManuallyEdited = isManuallyEdited;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a league from external provider data.
    /// </summary>
    /// <param name="sportId">The sport to which the league belongs.</param>
    /// <param name="name">The display name of the league.</param>
    /// <param name="country">The country in which the league operates, if known.</param>
    /// <param name="providerName">The name of the external data provider.</param>
    /// <param name="providerId">The identifier assigned by the external data provider.</param>
    /// <returns>A successful result containing the new league, or a failure when validation fails.</returns>
    public static Result<League> CreateFromProvider(
        SportId sportId,
        string name,
        string? country,
        string providerName,
        string providerId)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<League>(nameValidation.Error);

        var providerValidation = ValidateProvider(providerName, providerId);
        if (providerValidation.IsFailure)
            return Result.Failure<League>(providerValidation.Error);

        return new League(
            LeagueId.New(),
            sportId,
            name,
            country,
            providerName,
            providerId,
            false);
    }

    /// <summary>
    /// Creates a league entered manually without external provider linkage.
    /// </summary>
    /// <param name="sportId">The sport to which the league belongs.</param>
    /// <param name="name">The display name of the league.</param>
    /// <param name="country">The country in which the league operates, if known.</param>
    /// <returns>A successful result containing the new league, or a failure when validation fails.</returns>
    public static Result<League> CreateManually(SportId sportId, string name, string? country)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<League>(nameValidation.Error);

        return new League(
            LeagueId.New(),
            sportId,
            name,
            country,
            null,
            null,
            true);
    }

    /// <summary>
    /// Updates the league with data received from an external provider.
    /// </summary>
    /// <param name="name">The display name of the league.</param>
    /// <param name="country">The country in which the league operates, if known.</param>
    /// <param name="sportId">The sport to which the league belongs.</param>
    /// <returns>A successful result when synchronization succeeds; otherwise, a failure when the league is locked, deleted, or validation fails.</returns>
    public Result SyncExternalData(string name, string? country, SportId sportId)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var syncGuard = CheckRule(new ManuallyEditedLeagueCannotBeSyncedRule(this));
        if (syncGuard.IsFailure)
            return syncGuard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(name, country, sportId);

        return Result.Success();
    }

    /// <summary>
    /// Updates the league with manually entered data and locks it from external synchronization.
    /// </summary>
    /// <param name="name">The display name of the league.</param>
    /// <param name="country">The country in which the league operates, if known.</param>
    /// <param name="sportId">The sport to which the league belongs.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the league cannot be modified or validation fails.</returns>
    public Result UpdateManually(string name, string? country, SportId sportId)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(name, country, sportId);
        IsManuallyEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the league.
    /// </summary>
    /// <returns>A successful result. Idempotent when the league is already deleted.</returns>
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
            ? Result.Failure(Errors.League.Deleted)
            : Result.Success();

    private static Result ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result.Failure(Errors.League.EmptyName)
            : Result.Success();

    private static Result ValidateProvider(string providerName, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return Result.Failure(Errors.League.EmptyProviderName);

        if (string.IsNullOrWhiteSpace(providerId))
            return Result.Failure(Errors.League.EmptyProviderId);

        return Result.Success();
    }

    private void UpdateCoreFields(string name, string? country, SportId sportId)
    {
        Name = name;
        Country = country;
        SportId = sportId;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="League"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private League() { }
#pragma warning restore CS8618
}
