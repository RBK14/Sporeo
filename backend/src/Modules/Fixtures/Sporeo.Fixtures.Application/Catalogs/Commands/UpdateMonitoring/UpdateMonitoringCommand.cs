using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

/// <summary>
/// Command that creates or updates monitored sports and leagues from the admin catalog selection.
/// </summary>
/// <param name="Sports">The sports and nested leagues whose monitoring status should be applied.</param>
public sealed record UpdateMonitoringCommand(IReadOnlyList<UpdateMonitoringSportDto> Sports) : ICommand;
