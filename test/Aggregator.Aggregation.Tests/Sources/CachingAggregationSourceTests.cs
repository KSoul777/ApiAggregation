using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Aggregator.Aggregation.Infrastructure.Sources;
using Aggregator.Aggregation.Tests.TestDoubles;
using AwesomeAssertions;
using Microsoft.Extensions.Options;

namespace Aggregator.Aggregation.Tests.Sources;

public class CachingAggregationSourceTests
{
    private static CachingAggregationSource Wrap(
        IAggregationSource inner,
        FakeCacheService cache,
        bool enabled = true) =>
        new(inner, cache, Options.Create(new CacheOptions { Enabled = enabled, Ttl = TimeSpan.FromMinutes(1) }));

    [Fact]
    public async Task SecondIdenticalFetch_IsServedFromCache_WithoutCallingInner()
    {
        var cache = new FakeCacheService();
        var inner = new StubAggregationSource("github", "repository", [TestItems.Create()]);
        CachingAggregationSource caching = Wrap(inner, cache);

        var request = new SourceRequest { SearchTerm = "dotnet", MaxItems = 5 };

        await caching.FetchAsync(request, CancellationToken.None);
        IReadOnlyList<AggregatedItem> second = await caching.FetchAsync(request, CancellationToken.None);

        inner.CallCount.Should().Be(1);
        second.Should().HaveCount(1);
    }

    [Fact]
    public async Task DifferentRequest_MissesCache_AndCallsInnerAgain()
    {
        var cache = new FakeCacheService();
        var inner = new StubAggregationSource("github", "repository", [TestItems.Create()]);
        CachingAggregationSource caching = Wrap(inner, cache);

        await caching.FetchAsync(new SourceRequest { SearchTerm = "dotnet" }, CancellationToken.None);
        await caching.FetchAsync(new SourceRequest { SearchTerm = "rust" }, CancellationToken.None);

        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task WhenDisabled_AlwaysCallsInner_AndNeverWritesToCache()
    {
        var cache = new FakeCacheService();
        var inner = new StubAggregationSource("github", "repository", [TestItems.Create()]);
        CachingAggregationSource caching = Wrap(inner, cache, enabled: false);

        var request = new SourceRequest { SearchTerm = "dotnet" };
        await caching.FetchAsync(request, CancellationToken.None);
        await caching.FetchAsync(request, CancellationToken.None);

        inner.CallCount.Should().Be(2);
        cache.SetCount.Should().Be(0);
    }

    [Fact]
    public async Task FailedFetch_IsNotCached()
    {
        var cache = new FakeCacheService();
        var inner = new StubAggregationSource("github", "repository", [], new HttpRequestException("boom"));
        CachingAggregationSource caching = Wrap(inner, cache);

        var request = new SourceRequest { SearchTerm = "dotnet" };

        Func<Task> act = async () => await caching.FetchAsync(request, CancellationToken.None);
        await act.Should().ThrowAsync<HttpRequestException>();
        await act.Should().ThrowAsync<HttpRequestException>();

        inner.CallCount.Should().Be(2);
        cache.SetCount.Should().Be(0);
    }

    [Fact]
    public async Task SourcesWithTheSameRequest_DoNotShareCacheEntries()
    {
        var cache = new FakeCacheService();
        var github = new StubAggregationSource("github", "repository", [TestItems.Create()]);
        var news = new StubAggregationSource("spaceflight-news", "news", [TestItems.Create()]);

        var request = new SourceRequest { SearchTerm = "dotnet" };

        await Wrap(github, cache).FetchAsync(request, CancellationToken.None);
        await Wrap(news, cache).FetchAsync(request, CancellationToken.None);

        github.CallCount.Should().Be(1);
        news.CallCount.Should().Be(1);
        cache.SetCount.Should().Be(2);
    }
}
