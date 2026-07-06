namespace Sporeo.BuildingBlocks.Domain.Models;

public abstract record TypedIdBase
{
    public Guid Value { get; }

    protected TypedIdBase(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ID value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
