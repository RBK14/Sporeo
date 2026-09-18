using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadStores;

/// <summary>
/// Represents a read-model for a league with a current season.
/// </summary>
/// <param name="LeagueId"></param>
/// <param name="ExternalProviderName"></param>
/// <param name="ExternalProviderId"></param>
/// <param name="CurrentSeasonName"></param>
public sealed record LeagueWithCurrentSeasonReadModel(
    LeagueId LeagueId,
    string ExternalProviderName,
    string ExternalProviderId,
    string CurrentSeasonName);
