using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

/// <summary>
/// Sport selection payload for <see cref="UpdateMonitoringCommand"/>.
/// </summary>
/// <param name="Id">The local sport identifier when the sport already exists; otherwise <see langword="null"/>.</param>
/// <param name="ProviderId">The sport identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Leagues">The leagues under this sport whose monitoring status should be applied.</param>
public sealed record UpdateMonitoringSportDto(
    SportId? Id,
    string ProviderId,
    string ProviderName,
    IReadOnlyList<UpdateMonitoringLeagueDto> Leagues);

/// <summary>
/// League selection payload for <see cref="UpdateMonitoringCommand"/>.
/// </summary>
/// <param name="Id">The local league identifier when the league already exists; otherwise <see langword="null"/>.</param>
/// <param name="ProviderId">The league identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="IsMonitored">Whether the league should be actively monitored for synchronization.</param>
public sealed record UpdateMonitoringLeagueDto(
    LeagueId? Id,
    string ProviderId,
    string ProviderName,
    bool IsMonitored);
