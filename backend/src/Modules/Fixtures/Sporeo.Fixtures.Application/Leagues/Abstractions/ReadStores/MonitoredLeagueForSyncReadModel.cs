using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadStores;

/// <summary>
/// Represents a read-model for a monitored league that requires synchronization.
/// </summary>
/// <param name="LeagueId"></param>
/// <param name="SportId"></param>
/// <param name="ExternalProviderName"></param>
/// <param name="ExternalProviderId"></param>
public sealed record MonitoredLeagueForSyncReadModel(
    LeagueId LeagueId,
    SportId SportId,
    string ExternalProviderName,
    string ExternalProviderId);
