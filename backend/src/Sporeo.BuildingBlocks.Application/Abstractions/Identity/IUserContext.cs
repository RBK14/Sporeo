namespace Sporeo.BuildingBlocks.Application.Abstractions.Identity;

/// <summary>
/// Provides access to the identity of the user executing the current request.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
