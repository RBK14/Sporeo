namespace Sporeo.BuildingBlocks.Domain.Models;

public interface IDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedOn { get; }
}
