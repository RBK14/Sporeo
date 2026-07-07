using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Domain.Rules;

/// <summary>
/// Defines a domain business rule that can be evaluated for violations.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Determines whether the business rule is currently violated.
    /// </summary>
    /// <returns><see langword="true"/> if the rule is broken; otherwise, <see langword="false"/>.</returns>
    bool IsBroken();

    /// <summary>
    /// Gets the error that describes the rule violation.
    /// </summary>
    Error Error { get; }
}
