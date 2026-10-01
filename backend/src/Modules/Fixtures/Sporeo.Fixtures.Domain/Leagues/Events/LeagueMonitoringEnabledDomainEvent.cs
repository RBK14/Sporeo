using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Leagues.Events;

/// <summary>
/// Raised when a league becomes monitored for external fixture synchronization.
/// </summary>
/// <param name="LeagueId">The identifier of the league that entered the monitored state.</param>
public sealed record LeagueMonitoringEnabledDomainEvent(LeagueId LeagueId) : DomainEvent;
