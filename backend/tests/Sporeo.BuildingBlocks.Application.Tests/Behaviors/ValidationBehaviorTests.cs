using FluentAssertions;
using FluentValidation;
using MediatR;
using Sporeo.BuildingBlocks.Application.Behaviors;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record TestCommand(string Name) : IRequest<Result>;

    private sealed record TestQuery(string Name) : IRequest<Result<string>>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode("Name.Required")
                .WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldInvokeNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);
        var invoked = false;

        var result = await behavior.Handle(
            new TestCommand("valid"),
            _ =>
            {
                invoked = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        invoked.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidResultCommand_ShouldReturnValidationFailure()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);

        var result = await behavior.Handle(
            new TestCommand(string.Empty),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.Errors.Should().ContainSingle(error =>
            error.Code == "Name.Required" &&
            error.Message == "Name is required.");
    }

    [Fact]
    public async Task Handle_WithInvalidResultOfT_ShouldReturnValidationFailure()
    {
        var behavior = new ValidationBehavior<TestQuery, Result<string>>([new TestQueryValidator()]);

        var result = await behavior.Handle(
            new TestQuery(string.Empty),
            _ => Task.FromResult(Result.Success("value")),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    private sealed record UnsupportedCommand(string Name) : IRequest<string>;

    private sealed class UnsupportedCommandValidator : AbstractValidator<UnsupportedCommand>
    {
        public UnsupportedCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task Handle_WithUnsupportedResponseType_ShouldThrow()
    {
        var behavior = new ValidationBehavior<UnsupportedCommand, string>([new UnsupportedCommandValidator()]);

        var act = () => behavior.Handle(
            new UnsupportedCommand(string.Empty),
            _ => Task.FromResult("value"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Result*");
    }

    private sealed class TestQueryValidator : AbstractValidator<TestQuery>
    {
        public TestQueryValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithErrorCode("Name.Required");
        }
    }
}
