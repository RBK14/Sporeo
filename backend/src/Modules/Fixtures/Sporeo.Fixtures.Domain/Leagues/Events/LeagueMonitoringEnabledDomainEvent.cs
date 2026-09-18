using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Leagues.Events;

public sealed record LeagueMonitoringEnabledDomainEvent(LeagueId LeagueId) : DomainEvent;
