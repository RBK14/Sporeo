using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Sports.Commands.CreateSport;

/// <summary>
/// Creates a sport aggregate.
/// </summary>
/// <param name="Name">The sport display name.</param>
public sealed record CreateSportCommand(string Name) : ICommand<Guid>;
