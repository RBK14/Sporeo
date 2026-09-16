namespace Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

/// <summary>
/// Synchronization horizon used by fixture sync jobs.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Fetches recently finished and upcoming fixtures for a league.
    /// </summary>
    ShortTerm = 0,

    /// <summary>
    /// Fetches the full season schedule for a league.
    /// </summary>
    LongTerm = 1
}
