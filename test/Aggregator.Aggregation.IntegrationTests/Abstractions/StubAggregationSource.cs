using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.IntegrationTests.Abstractions;

internal sealed class StubAggregationSource(
    string name,
    string category,
    IReadOnlyList<AggregatedItem> items)
    : IAggregationSource
{
    public string Name => name;

    public string Category => category;

    public Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken) => Task.FromResult(items);
}
