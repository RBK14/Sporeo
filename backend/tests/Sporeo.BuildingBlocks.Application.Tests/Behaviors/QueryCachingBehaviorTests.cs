using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Behaviors;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Tests.Behaviors;

public class QueryCachingBehaviorTests
{
    private sealed record TestCachedQuery(string CacheKey, TimeSpan? Expiration) : ICachedQuery;

    [Fact]
    public async Task Handle_WithCacheHit_ShouldReturnCachedResponse()
    {
        var cacheService = Substitute.For<ICacheService>();
        var cached = Result.Success("cached");
        cacheService
            .GetAsync<Result<string>>("cache-key", Arg.Any<CancellationToken>())
            .Returns(cached);

        var behavior = new QueryCachingBehavior<TestCachedQuery, Result<string>>(
            cacheService,
            NullLogger<QueryCachingBehavior<TestCachedQuery, Result<string>>>.Instance);

        var result = await behavior.Handle(
            new TestCachedQuery("cache-key", TimeSpan.FromMinutes(5)),
            _ => Task.FromResult(Result.Success("fresh")),
            CancellationToken.None);

        result.Should().Be(cached);
        await cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<Result<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCacheMissAndSuccess_ShouldStoreResponse()
    {
        var cacheService = Substitute.For<ICacheService>();
        cacheService
            .GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        var behavior = new QueryCachingBehavior<TestCachedQuery, Result<string>>(
            cacheService,
            NullLogger<QueryCachingBehavior<TestCachedQuery, Result<string>>>.Instance);

        var result = await behavior.Handle(
            new TestCachedQuery("cache-key", TimeSpan.FromMinutes(5)),
            _ => Task.FromResult(Result.Success("fresh")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cacheService.Received(1).SetAsync(
            "cache-key",
            result,
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCacheMissAndFailure_ShouldNotStoreResponse()
    {
        var cacheService = Substitute.For<ICacheService>();
        cacheService
            .GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        var behavior = new QueryCachingBehavior<TestCachedQuery, Result<string>>(
            cacheService,
            NullLogger<QueryCachingBehavior<TestCachedQuery, Result<string>>>.Instance);

        var result = await behavior.Handle(
            new TestCachedQuery("cache-key", TimeSpan.FromMinutes(5)),
            _ => Task.FromResult(Result.Failure<string>(new Error("Test.Code", "Failed"))),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<Result<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}
