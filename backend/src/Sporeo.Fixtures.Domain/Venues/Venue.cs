using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Venues.Rules;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Venues;

/// <summary>
/// Represents a sporting venue with optional address, coordinates, and external provider linkage.
/// </summary>
public sealed class Venue : AggregateRoot<VenueId>, IAuditable, IDeletable
{
    /// <summary>
    /// Gets the display name of the venue.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the postal address of the venue, if specified.
    /// </summary>
    public Address? Address { get; private set; }

    /// <summary>
    /// Gets the geographic coordinates of the venue, if specified.
    /// </summary>
    public Coordinates? Coordinates { get; private set; }

    /// <summary>
    /// Gets the name of the external data provider, if the venue originated from synchronization.
    /// </summary>
    public string? ExternalProviderName { get; private set; }

    /// <summary>
    /// Gets the identifier assigned by the external data provider, if the venue originated from synchronization.
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the venue has been manually edited and is locked from external synchronization.
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

    private Venue(
        VenueId id,
        string name,
        Address? address,
        Coordinates? coordinates,
        string? externalProviderName,
        string? externalProviderId,
        bool isManuallyEdited) : base(id)
    {
        Name = name;
        Address = address;
        Coordinates = coordinates;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        IsManuallyEdited = isManuallyEdited;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a venue from external provider data.
    /// </summary>
    /// <param name="name">The display name of the venue.</param>
    /// <param name="providerName">The name of the external data provider.</param>
    /// <param name="providerId">The identifier assigned by the external data provider.</param>
    /// <param name="address">The postal address of the venue, if known.</param>
    /// <param name="coordinates">The geographic coordinates of the venue, if known.</param>
    /// <returns>A successful result containing the new venue, or a failure when validation fails.</returns>
    public static Result<Venue> CreateFromProvider(
        string name,
        string providerName,
        string providerId,
        Address? address = null,
        Coordinates? coordinates = null)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Venue>(nameValidation.Error);

        var providerValidation = ValidateProvider(providerName, providerId);
        if (providerValidation.IsFailure)
            return Result.Failure<Venue>(providerValidation.Error);

        return new Venue(
            VenueId.New(),
            name,
            address,
            coordinates,
            providerName,
            providerId,
            false);
    }

    /// <summary>
    /// Creates a venue entered manually without external provider linkage.
    /// </summary>
    /// <param name="name">The display name of the venue.</param>
    /// <param name="address">The postal address of the venue, if known.</param>
    /// <param name="coordinates">The geographic coordinates of the venue, if known.</param>
    /// <returns>A successful result containing the new venue, or a failure when validation fails.</returns>
    public static Result<Venue> CreateManually(
        string name,
        Address? address = null,
        Coordinates? coordinates = null)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Venue>(nameValidation.Error);

        return new Venue(
            VenueId.New(),
            name,
            address,
            coordinates,
            null,
            null,
            true);
    }

    /// <summary>
    /// Updates the venue with data received from an external provider.
    /// </summary>
    /// <param name="name">The display name of the venue.</param>
    /// <param name="address">The postal address of the venue, if known.</param>
    /// <param name="coordinates">The geographic coordinates of the venue, if known.</param>
    /// <returns>A successful result when synchronization succeeds; otherwise, a failure when the venue is locked, deleted, or validation fails.</returns>
    public Result SyncExternalData(
        string name,
        Address? address = null,
        Coordinates? coordinates = null)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var syncGuard = CheckRule(new ManuallyEditedVenueCannotBeSyncedRule(this));
        if (syncGuard.IsFailure)
            return syncGuard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(name, address, coordinates);

        return Result.Success();
    }

    /// <summary>
    /// Updates the venue with manually entered data and locks it from external synchronization.
    /// </summary>
    /// <param name="name">The display name of the venue.</param>
    /// <param name="address">The postal address of the venue, if known.</param>
    /// <param name="coordinates">The geographic coordinates of the venue, if known.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the venue cannot be modified or validation fails.</returns>
    public Result UpdateManually(
        string name,
        Address? address = null,
        Coordinates? coordinates = null)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        UpdateCoreFields(name, address, coordinates);
        IsManuallyEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the venue.
    /// </summary>
    /// <returns>A successful result. Idempotent when the venue is already deleted.</returns>
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
            ? Result.Failure(Errors.Venue.Deleted)
            : Result.Success();

    private static Result ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result.Failure(Errors.Venue.EmptyName)
            : Result.Success();

    private static Result ValidateProvider(string providerName, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return Result.Failure(Errors.Venue.EmptyProviderName);

        if (string.IsNullOrWhiteSpace(providerId))
            return Result.Failure(Errors.Venue.EmptyProviderId);

        return Result.Success();
    }

    private void UpdateCoreFields(
        string name,
        Address? address,
        Coordinates? coordinates)
    {
        Name = name;
        Address = address;
        Coordinates = coordinates;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Venue"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Venue() { }
#pragma warning restore CS8618
}
