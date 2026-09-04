using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Application.Feed.GetFeed;
using Aggregator.Aggregation.Tests.TestDoubles;
using AwesomeAssertions;

namespace Aggregator.Aggregation.Tests.Feed;

public class FeedQueryProcessorTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day3 = new(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Filter_ByCategory_IsCaseInsensitive_AndKeepsOnlyMatches()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(category: "weather"),
            TestItems.Create(category: "news"),
            TestItems.Create(category: "News")
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Filter(items, new GetFeedQuery { Category = "news" });

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => string.Equals(i.Category, "news", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filter_BySource_KeepsOnlyMatchingSource()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(source: "github"),
            TestItems.Create(source: "open-meteo")
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Filter(items, new GetFeedQuery { Source = "GITHUB" });

        result.Should().ContainSingle().Which.Source.Should().Be("github");
    }

    [Fact]
    public void Filter_ByDateRange_ExcludesOutOfRange_AndNullTimestamps()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(title: "d1", timestamp: Day1),
            TestItems.Create(title: "d2", timestamp: Day2),
            TestItems.Create(title: "d3", timestamp: Day3),
            TestItems.Create(title: "none", timestamp: null)
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Filter(items, new GetFeedQuery { From = Day2, To = Day3 });

        result.Select(i => i.Title).Should().BeEquivalentTo("d2", "d3");
    }

    [Fact]
    public void Sort_ByTimestampDescending_PutsNewestFirst_AndNullsLast()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(title: "old", timestamp: Day1),
            TestItems.Create(title: "none", timestamp: null),
            TestItems.Create(title: "new", timestamp: Day3)
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Sort(items, FeedSortField.Timestamp, SortDirection.Descending);

        result.Select(i => i.Title).Should().ContainInOrder("new", "old", "none");
    }

    [Fact]
    public void Sort_ByTimestampAscending_KeepsNullsLast()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(title: "none", timestamp: null),
            TestItems.Create(title: "new", timestamp: Day3),
            TestItems.Create(title: "old", timestamp: Day1)
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Sort(items, FeedSortField.Timestamp, SortDirection.Ascending);

        result.Select(i => i.Title).Should().ContainInOrder("old", "new", "none");
    }

    [Fact]
    public void Sort_ByRelevanceDescending_OrdersByScore()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(title: "low", relevance: 10),
            TestItems.Create(title: "high", relevance: 100),
            TestItems.Create(title: "mid", relevance: 50)
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Sort(items, FeedSortField.Relevance, SortDirection.Descending);

        result.Select(i => i.Title).Should().ContainInOrder("high", "mid", "low");
    }

    [Fact]
    public void Sort_ByTitleAscending_IsCaseInsensitive()
    {
        AggregatedItem[] items =
        [
            TestItems.Create(title: "banana"),
            TestItems.Create(title: "Apple"),
            TestItems.Create(title: "cherry")
        ];

        IReadOnlyList<AggregatedItem> result =
            FeedQueryProcessor.Sort(items, FeedSortField.Title, SortDirection.Ascending);

        result.Select(i => i.Title).Should().ContainInOrder("Apple", "banana", "cherry");
    }

    [Fact]
    public void Apply_FiltersThenSortsThenPages_AndReportsTotalCount()
    {
        var items = Enumerable.Range(1, 25)
            .Select(n => TestItems.Create(
                category: "news",
                title: $"item-{n:D2}",
                timestamp: Day1.AddMinutes(n)))
            .ToList();

        var query = new GetFeedQuery
        {
            Category = "news",
            SortBy = FeedSortField.Timestamp,
            Order = SortDirection.Descending,
            Page = 2,
            PageSize = 10
        };

        (IReadOnlyList<AggregatedItem> pageItems, int totalCount) = FeedQueryProcessor.Apply(items, query);

        totalCount.Should().Be(25);
        pageItems.Should().HaveCount(10);
        // Newest first: page 1 = 25..16, page 2 = 15..6.
        pageItems[0].Title.Should().Be("item-15");
        pageItems[^1].Title.Should().Be("item-06");
    }

    [Fact]
    public void Apply_LastPage_ReturnsRemainder()
    {
        var items = Enumerable.Range(1, 25)
            .Select(n => TestItems.Create(title: $"item-{n:D2}"))
            .ToList();

        var query = new GetFeedQuery { Page = 3, PageSize = 10, SortBy = FeedSortField.Title, Order = SortDirection.Ascending };

        (IReadOnlyList<AggregatedItem> pageItems, int totalCount) = FeedQueryProcessor.Apply(items, query);

        totalCount.Should().Be(25);
        pageItems.Should().HaveCount(5);
    }
}
