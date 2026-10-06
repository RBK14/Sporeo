using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Venues.Enums;
using Sporeo.Fixtures.Domain.Venues.Events;
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

    /// <summary>
    /// Gets the outcome of resolving geographic coordinates for the venue.
    /// </summary>
    public GeocodingStatus GeocodingStatus { get; private set; }

    /// <summary>
    /// Gets the error code reported by the last failed geocoding attempt, if any.
    /// </summary>
    public string? GeocodingErrorCode { get; private set; }

    /// <summary>
    /// Gets the date and time of the last geocoding attempt, if any.
    /// </summary>
    public DateTimeOffset? LastGeocodingAttemptOn { get; private set; }

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
        GeocodingStatus = ResolveGeocodingStatus(coordinates);
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

        var venue = new Venue(
            VenueId.New(),
            name,
            address,
            coordinates,
            providerName,
            providerId,
            false);

        if (coordinates is null)
            venue.AddDomainEvent(new VenueCreatedDomainEvent(venue.Id));

        return venue;
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

        var venue = new Venue(
            VenueId.New(),
            name,
            address,
            coordinates,
            null,
            null,
            true);

        if (coordinates is null)
            venue.AddDomainEvent(new VenueCreatedDomainEvent(venue.Id));

        return venue;
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
    /// Applies a location resolved by geocoding. Manually edited venues receive the coordinates
    /// but keep their manually entered address.
    /// </summary>
    /// <param name="geocodedAddress">The address returned by the geocoding service, if any.</param>
    /// <param name="coordinates">The geographic coordinates resolved by the geocoding service.</param>
    /// <returns>A successful result when the location is applied; otherwise, a failure when the venue is deleted.</returns>
    public Result ApplyGeocodedLocation(Address? geocodedAddress, Coordinates coordinates)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        if (!IsManuallyEdited)
            Address = geocodedAddress ?? Address;

        ApplyCoordinates(coordinates);
        LastGeocodingAttemptOn = SystemTimeProvider.Now;

        return Result.Success();
    }

    /// <summary>
    /// Records a failed geocoding attempt so administrators can review venues without coordinates.
    /// </summary>
    /// <param name="status">The failure outcome; must be <see cref="GeocodingStatus.NotFound"/> or <see cref="GeocodingStatus.Failed"/>.</param>
    /// <param name="errorCode">The error code reported by the geocoding service.</param>
    /// <returns>A successful result when the failure is recorded; otherwise, a failure when the venue is deleted or the status is not a failure outcome.</returns>
    public Result MarkGeocodingFailed(GeocodingStatus status, string errorCode)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        if (status is not (GeocodingStatus.NotFound or GeocodingStatus.Failed))
            return Result.Failure(Errors.Venue.InvalidGeocodingFailureStatus);

        GeocodingStatus = status;
        GeocodingErrorCode = errorCode;
        LastGeocodingAttemptOn = SystemTimeProvider.Now;

        return Result.Success();
    }

    /// <summary>
    /// Unlocks the venue for external synchronization, allowing future updates from providers to be applied.
    /// </summary>
    /// <returns>A successful result when the venue is unlocked; otherwise, a failure when the venue cannot be modified.</returns>
    public Result UnlockForSync()
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure) return guard;

        IsManuallyEdited = false;
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
        ApplyCoordinates(coordinates);
    }

    /// <summary>
    /// Updates coordinates and the geocoding status. When the venue stays without coordinates,
    /// the recorded geocoding outcome is preserved so routine synchronization does not erase it.
    /// </summary>
    private void ApplyCoordinates(Coordinates? coordinates)
    {
        if (coordinates is null && Coordinates is null)
            return;

        Coordinates = coordinates;
        GeocodingStatus = ResolveGeocodingStatus(coordinates);
        GeocodingErrorCode = null;
    }

    private static GeocodingStatus ResolveGeocodingStatus(Coordinates? coordinates) =>
        coordinates is null ? GeocodingStatus.Pending : GeocodingStatus.Resolved;

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Venue"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Venue() { }
#pragma warning restore CS8618
}
