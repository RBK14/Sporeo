namespace Sporeo.BuildingBlocks.Domain.Results;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static readonly Error NullValue = new("Error.NullValue", "A given value cannot be null or empty.");
}
