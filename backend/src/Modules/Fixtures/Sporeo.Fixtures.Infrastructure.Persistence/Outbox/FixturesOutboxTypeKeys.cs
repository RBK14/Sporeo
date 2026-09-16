namespace Sporeo.Fixtures.Infrastructure.Persistence.Outbox;

/// <summary>
/// Durable outbox type keys owned by the Fixtures module.
/// </summary>
public static class FixturesOutboxTypeKeys
{
    /// <summary>
    /// Durable outbox type key for <c>VenueCreatedDomainEvent</c>.
    /// </summary>
    public const string VenueCreatedDomainEvent = "Sporeo.Fixtures.VenueCreatedDomainEvent.v1";
}
