namespace Sporeo.BuildingBlocks.Domain.Models;

public interface IAuditable
{
    DateTimeOffset CreatedOn { get; }
    DateTimeOffset? ModifiedOn { get; }
}
