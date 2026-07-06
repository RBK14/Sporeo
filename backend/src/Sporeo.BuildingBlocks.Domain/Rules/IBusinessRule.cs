using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Domain.Rules;

public interface IBusinessRule
{
    bool IsBroken();
    Error Error { get; }
}
