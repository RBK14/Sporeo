namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

public sealed record UpdateMonitoringSportDto(
    string ProviderId,
    string ProviderName,
    IReadOnlyList<UpdateMonitoringLeagueDto> Leagues);

public sealed record UpdateMonitoringLeagueDto(
    string ProviderId,
    string ProviderName,
    bool IsMonitored);