using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadStores;

public sealed record MonitoredLeagueForSyncReadModel(
    LeagueId LeagueId,
    SportId SportId,
    string ExternalProviderName,
    string ExternalProviderId);
