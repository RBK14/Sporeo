namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadStores;

public sealed record LeagueExternalProviderDataReadModel(
    string ExternalProviderName,
    string ExternalProviderId);
