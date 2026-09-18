using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Fixtures.Abstractions;

/// <summary>
/// Port for fetching fixtures from an external sports data provider.
/// </summary>
public interface IExternalFixturesClient
{
    /// <summary>
    /// Gets the stable provider name used for identity and configuration matching.
    /// </summary>
    string ProviderName { get; }

    Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchShortTermFixturesAsync(
        string externalLeagueId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchLongTermFixturesAsync(
        string externalLeagueId,
        string seasonName,
        CancellationToken cancellationToken = default);
}
