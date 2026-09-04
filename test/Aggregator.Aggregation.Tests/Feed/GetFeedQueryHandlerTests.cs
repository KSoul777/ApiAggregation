using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Application.Feed.GetFeed;
using Aggregator.Aggregation.Tests.TestDoubles;
using Aggregator.Common.Domain;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aggregator.Aggregation.Tests.Feed;

public class GetFeedQueryHandlerTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private static GetFeedQueryHandler CreateHandler(params IAggregationSource[] sources) =>
        new(sources, NullLogger<GetFeedQueryHandler>.Instance);

    [Fact]
    public async Task Handle_AggregatesItemsFromEverySource_AndCallsEachOnce()
    {
        var weather = new StubAggregationSource("open-meteo", "weather",
            [TestItems.Create(source: "open-meteo", category: "weather", timestamp: Day1)]);
        var news = new StubAggregationSource("news", "news",
            [TestItems.Create(source: "news", category: "news", timestamp: Day2)]);

        GetFeedQueryHandler handler = CreateHandler(weather, news);

        Result<FeedResponse> result = await handler.Handle(new GetFeedQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        weather.CallCount.Should().Be(1);
        news.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenOneSourceFails_DegradesGracefully_AndKeepsOthers()
    {
        var healthy = new StubAggregationSource("news", "news",
            [TestItems.Create(source: "news", category: "news", timestamp: Day1)]);
        var failing = new StubAggregationSource("github", "repository",
            [], new HttpRequestException("upstream exploded"));

        GetFeedQueryHandler handler = CreateHandler(healthy, failing);

        Result<FeedResponse> result = await handler.Handle(new GetFeedQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle().Which.Source.Should().Be("news");

        SourceOutcome failure = result.Value.Sources.Single(s => s.Source == "github");
        failure.Succeeded.Should().BeFalse();
        failure.ItemCount.Should().Be(0);
        failure.Error.Should().Contain("upstream exploded");

        result.Value.Sources.Single(s => s.Source == "news").Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AppliesSortingAndPaging_AcrossSources()
    {
        var a = new StubAggregationSource("a", "news",
            [TestItems.Create(source: "a", title: "older", timestamp: Day1)]);
        var b = new StubAggregationSource("b", "news",
            [TestItems.Create(source: "b", title: "newer", timestamp: Day2)]);

        GetFeedQueryHandler handler = CreateHandler(a, b);

        var query = new GetFeedQuery
        {
            SortBy = FeedSortField.Timestamp,
            Order = SortDirection.Descending,
            Page = 1,
            PageSize = 1
        };

        Result<FeedResponse> result = await handler.Handle(query, CancellationToken.None);

        result.Value.Items.Should().ContainSingle().Which.Title.Should().Be("newer");
        result.Value.TotalCount.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoSources_ReturnsEmptyFeed()
    {
        GetFeedQueryHandler handler = CreateHandler();

        Result<FeedResponse> result = await handler.Handle(new GetFeedQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.Sources.Should().BeEmpty();
    }
}
