using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Domain.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldNotHaveError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldRequireError()
    {
        var error = new Error("Test.Code", "Test message");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessWithValue_ShouldExposeValue()
    {
        var result = Result.Success("value");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    [Fact]
    public void FailureWithValue_ShouldThrowWhenAccessingValue()
    {
        var result = Result.Failure<string>(new Error("Test.Code", "Test message"));

        var act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromNull_ShouldReturnFailure()
    {
        Result<string> result = (string?)null;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void Failure_WithoutError_ShouldThrow()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }
}
