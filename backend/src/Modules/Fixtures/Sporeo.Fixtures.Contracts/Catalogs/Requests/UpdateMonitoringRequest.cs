namespace Sporeo.Fixtures.Contracts.Catalogs.Requests;

public sealed record UpdateMonitoringRequest(IReadOnlyList<UpdateMonitoringSportRequest> Sports);

public sealed record UpdateMonitoringSportRequest(
    string ProviderId,
    string ProviderName,
    IReadOnlyList<UpdateMonitoringLeagueRequest> Leagues);

public sealed record UpdateMonitoringLeagueRequest(
    string ProviderId,
    string ProviderName,
    bool IsMonitored);