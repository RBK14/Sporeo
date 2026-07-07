namespace Sporeo.BuildingBlocks.Application.Abstractions.Identity;

public class IUserContext
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
