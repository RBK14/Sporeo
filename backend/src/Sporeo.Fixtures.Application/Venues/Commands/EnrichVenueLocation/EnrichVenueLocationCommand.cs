using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;

/// <summary>
/// Enriches a venue with coordinates/address resolved through geocoding.
/// </summary>
/// <param name="VenueId">The venue identifier.</param>
/// <remarks>
/// Idempotent: venues that already have coordinates are skipped successfully.
/// Failures from geocoding or domain updates are propagated so the outbox can retry.
/// </remarks>
public sealed record EnrichVenueLocationCommand(Guid VenueId) : ICommand;
