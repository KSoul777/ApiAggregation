using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.Tests.TestDoubles;

internal static class TestItems
{
    public static AggregatedItem Create(
        string source = "src",
        string category = "news",
        string title = "title",
        DateTimeOffset? timestamp = null,
        double? relevance = null) =>
        new()
        {
            Source = source,
            Category = category,
            Title = title,
            Summary = null,
            Url = null,
            Timestamp = timestamp,
            Relevance = relevance
        };
}
