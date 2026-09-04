namespace Aggregator.Aggregation.Application.Feed;

public interface IAggregationSource
{
    string Name { get; }
    string Category { get; }
    Task<IReadOnlyList<AggregatedItem>> FetchAsync(SourceRequest request, CancellationToken cancellationToken);
}
