using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

public sealed record UpdateMonitoringCommand(IReadOnlyList<UpdateMonitoringSportDto> Sports) : ICommand;
