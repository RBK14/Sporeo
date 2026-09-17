namespace Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;

public sealed record ExternalLeagueDto(
    string ProviderId,
    string ProviderName,
    string ProviderSportName,
    string Name,
    string? Country);
