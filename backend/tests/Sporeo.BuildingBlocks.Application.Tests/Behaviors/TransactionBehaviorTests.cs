using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Behaviors;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record TestCommand : ICommand;

    [Fact]
    public async Task Handle_WithSuccessfulResult_ShouldCommitUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, Result>(
            unitOfWork,
            NullLogger<TransactionBehavior<TestCommand, Result>>.Instance);

        var result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFailedResult_ShouldSkipCommit()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, Result>(
            unitOfWork,
            NullLogger<TransactionBehavior<TestCommand, Result>>.Instance);

        var result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Failure(new Error("Test.Code", "Failed"))),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
