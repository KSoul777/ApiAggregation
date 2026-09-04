using Aggregator.Common.Application.Messaging;

namespace Aggregator.Aggregation.Application.Feed.GetFeed;

public sealed record GetFeedQuery : IQuery<FeedResponse>
{
    public string? SearchTerm { get; init; }
    public string? City { get; init; }
    public int MaxItemsPerSource { get; init; } = 20;
    public string? Category { get; init; }
    public string? Source { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public FeedSortField SortBy { get; init; } = FeedSortField.Timestamp;
    public SortDirection Order { get; init; } = SortDirection.Descending;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    
    public SourceRequest ToSourceRequest() =>
        new()
        {
            SearchTerm = SearchTerm,
            City = City,
            MaxItems = MaxItemsPerSource
        };
}
