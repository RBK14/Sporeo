namespace Sporeo.Fixtures.Contracts.Catalogs.Requests;

public sealed record UpdateMonitoringRequest(IReadOnlyList<UpdateMonitoringSportRequest> Sports);

public sealed record UpdateMonitoringSportRequest(
    Guid? Id,
    string ProviderId,
    string ProviderName,
    IReadOnlyList<UpdateMonitoringLeagueRequest> Leagues);

public sealed record UpdateMonitoringLeagueRequest(
    Guid? Id,
    string ProviderId,
    string ProviderName,
    bool IsMonitored);
