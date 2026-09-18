namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;

public sealed record MonitoredLeagueForSyncDto(
    Guid LeagueId,
    Guid SportId,
    string ExternalProviderName,
    string ExternalProviderId);
