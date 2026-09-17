namespace Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;

public sealed record ExternalSportDto(
    string ProviderId,
    string ProviderName,
    string Name);
