namespace Sporeo.BuildingBlocks.Domain.Results;

/// <summary>
/// Represents a domain or application error identified by a stable code and a human-readable message.
/// </summary>
/// <param name="Code">A stable, machine-readable error code.</param>
/// <param name="Message">A human-readable description of the error.</param>
public record Error(string Code, string Message)
{
    /// <summary>
    /// Represents the absence of an error. Used by successful <see cref="Result"/> instances.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error that occurs when a required value is <see langword="null"/> or empty.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "A given value cannot be null or empty.");
}

/// <summary>
/// Represents a validation failure that aggregates one or more field-level <see cref="Error"/> instances.
/// </summary>
/// <param name="Errors">The collection of validation errors.</param>
public record ValidationError(Error[] Errors)
    : Error("Error.ValidationFailure", "Wystąpiły błędy walidacji jednego lub więcej pól.");
