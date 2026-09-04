namespace Aggregator.Aggregation.Application.Feed.GetFeed;

public sealed record FeedResponse
{
    public required IReadOnlyList<AggregatedItem> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required IReadOnlyList<SourceOutcome> Sources { get; init; }
}
