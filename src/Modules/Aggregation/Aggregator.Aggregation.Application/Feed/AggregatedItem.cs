namespace Aggregator.Aggregation.Application.Feed;

public sealed record AggregatedItem
{
    public required string Source { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? Url { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
    public double? Relevance { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
