using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Sports.Commands.CreateSport;

/// <summary>
/// Creates a sport aggregate.
/// </summary>
/// <param name="Name">The sport display name.</param>
public sealed record CreateSportCommand(string Name) : ICommand<SportId>;
