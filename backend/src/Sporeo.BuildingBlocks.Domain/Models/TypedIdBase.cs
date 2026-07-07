namespace Sporeo.BuildingBlocks.Domain.Models;

/// <summary>
/// Base type for strongly typed identifiers backed by a <see cref="Guid"/> value.
/// </summary>
public abstract record TypedIdBase
{
    /// <summary>
    /// Gets the underlying <see cref="Guid"/> identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedIdBase"/> class.
    /// </summary>
    /// <param name="value">The identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    protected TypedIdBase(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ID value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
