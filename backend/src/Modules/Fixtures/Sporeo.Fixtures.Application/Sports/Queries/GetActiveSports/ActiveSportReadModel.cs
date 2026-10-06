using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;

/// <summary>
/// Lightweight sport item returned for public navigation menus.
/// </summary>
/// <param name="Id">The sport identifier.</param>
/// <param name="Name">The sport display name.</param>
public sealed record ActiveSportReadModel(SportId Id, string Name);
