using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Domain.Common;

/// <summary>
/// Defines stable domain error codes for the fixtures bounded context.
/// </summary>
public static class Errors
{
    /// <summary>
    /// Errors related to <see cref="Fixtures.Fixture"/> aggregates.
    /// </summary>
    public static class Fixture
    {
        /// <summary>
        /// Returned when a fixture name is missing or whitespace.
        /// </summary>
        public static readonly Error EmptyName = new("Fixture.EmptyName", "Fixture name cannot be empty.");

        /// <summary>
        /// Returned when an external provider name is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderName = new("Fixture.EmptyProviderName", "External provider name is required.");

        /// <summary>
        /// Returned when an external provider identifier is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderId = new("Fixture.EmptyProviderId", "External provider ID is required.");

        /// <summary>
        /// Returned when an operation targets a soft-deleted fixture.
        /// </summary>
        public static readonly Error Deleted = new("Fixture.Deleted", "Cannot modify a deleted fixture.");

        /// <summary>
        /// Returned when an operation attempts to modify a finished fixture.
        /// </summary>
        public static readonly Error Finished = new("Fixture.Finished", "Cannot modify a finished fixture.");

        /// <summary>
        /// Returned when external synchronization is attempted on a manually edited fixture.
        /// </summary>
        public static readonly Error LockedForSync = new("Fixture.LockedForSync", "Cannot synchronize a fixture that has been manually edited.");

        /// <summary>
        /// Returned when a fixture status transition is not allowed.
        /// </summary>
        public static readonly Error InvalidStatusTransition = new("Fixture.InvalidStatusTransition", "The requested status transition is not allowed.");
    }

    /// <summary>
    /// Errors related to <see cref="Venues.Venue"/> aggregates.
    /// </summary>
    public static class Venue
    {
        /// <summary>
        /// Returned when a venue name is missing or whitespace.
        /// </summary>
        public static readonly Error EmptyName = new("Venue.EmptyName", "Venue name cannot be empty.");

        /// <summary>
        /// Returned when an external provider name is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderName = new("Venue.EmptyProviderName", "External provider name is required.");

        /// <summary>
        /// Returned when an external provider identifier is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderId = new("Venue.EmptyProviderId", "External provider ID is required.");

        /// <summary>
        /// Returned when an operation targets a soft-deleted venue.
        /// </summary>
        public static readonly Error Deleted = new("Venue.Deleted", "Cannot modify a deleted venue.");

        /// <summary>
        /// Returned when external synchronization is attempted on a manually edited venue.
        /// </summary>
        public static readonly Error LockedForSync = new("Venue.LockedForSync", "Cannot synchronize a venue that has been manually edited.");

        /// <summary>
        /// Errors related to venue geographic coordinates.
        /// </summary>
        public static class Coordinates
        {
            /// <summary>
            /// Returned when latitude is outside the valid range of -90 to 90.
            /// </summary>
            public static readonly Error InvalidLatitude = new("Venue.Coordinates.InvalidLatitude", "Latitude must be between -90 and 90.");

            /// <summary>
            /// Returned when longitude is outside the valid range of -180 to 180.
            /// </summary>
            public static readonly Error InvalidLongitude = new("Venue.Coordinates.InvalidLongitude", "Longitude must be between -180 and 180.");
        }

        /// <summary>
        /// Errors related to venue postal addresses.
        /// </summary>
        public static class Address
        {
            /// <summary>
            /// Returned when a provided street value is empty or whitespace.
            /// </summary>
            public static readonly Error EmptyStreet = new("Venue.Address.EmptyStreet", "Street cannot be empty.");

            /// <summary>
            /// Returned when a provided city value is empty or whitespace.
            /// </summary>
            public static readonly Error EmptyCity = new("Venue.Address.EmptyCity", "City cannot be empty.");

            /// <summary>
            /// Returned when a provided country value is empty or whitespace.
            /// </summary>
            public static readonly Error EmptyCountry = new("Venue.Address.EmptyCountry", "Country cannot be empty.");
        }
    }

    /// <summary>
    /// Errors related to <see cref="Sports.Sport"/> aggregates.
    /// </summary>
    public static class Sport
    {
        /// <summary>
        /// Returned when a sport name is missing or whitespace.
        /// </summary>
        public static readonly Error EmptyName = new("Sport.EmptyName", "Sport name cannot be empty.");

        /// <summary>
        /// Returned when an operation targets a soft-deleted sport.
        /// </summary>
        public static readonly Error Deleted = new("Sport.Deleted", "Cannot modify a deleted sport.");
    }

    /// <summary>
    /// Errors related to <see cref="Leagues.League"/> aggregates.
    /// </summary>
    public static class League
    {
        /// <summary>
        /// Returned when a league name is missing or whitespace.
        /// </summary>
        public static readonly Error EmptyName = new("League.EmptyName", "League name cannot be empty.");

        /// <summary>
        /// Returned when an external provider name is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderName = new("League.EmptyProviderName", "External provider name is required.");

        /// <summary>
        /// Returned when an external provider identifier is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderId = new("League.EmptyProviderId", "External provider ID is required.");

        /// <summary>
        /// Returned when an operation targets a soft-deleted league.
        /// </summary>
        public static readonly Error Deleted = new("League.Deleted", "Cannot modify a deleted league.");

        /// <summary>
        /// Returned when external synchronization is attempted on a manually edited league.
        /// </summary>
        public static readonly Error LockedForSync = new("League.LockedForSync", "Cannot synchronize a league that has been manually edited.");
    }

    /// <summary>
    /// Errors related to <see cref="Seasons.Season"/> aggregates.
    /// </summary>
    public static class Season
    {
        /// <summary>
        /// Returned when a season name is missing or whitespace.
        /// </summary>
        public static readonly Error EmptyName = new("Season.EmptyName", "Season name cannot be empty.");

        /// <summary>
        /// Returned when an external provider name is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderName = new("Season.EmptyProviderName", "External provider name is required.");

        /// <summary>
        /// Returned when an external provider identifier is required but missing.
        /// </summary>
        public static readonly Error EmptyProviderId = new("Season.EmptyProviderId", "External provider ID is required.");

        /// <summary>
        /// Returned when a season start date is not before its end date.
        /// </summary>
        public static readonly Error InvalidDateRange = new("Season.InvalidDateRange", "Season start date must be before end date.");

        /// <summary>
        /// Returned when an operation targets a soft-deleted season.
        /// </summary>
        public static readonly Error Deleted = new("Season.Deleted", "Cannot modify a deleted season.");

        /// <summary>
        /// Returned when external synchronization is attempted on a manually edited season.
        /// </summary>
        public static readonly Error LockedForSync = new("Season.LockedForSync", "Cannot synchronize a season that has been manually edited.");
    }
}
