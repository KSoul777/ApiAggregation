using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.Tests.TestDoubles;

/// <summary>
/// Configurable <see cref="IAggregationSource"/> test double that records how many times it was
/// called and can either return a fixed set of items or throw a configured exception.
/// </summary>
internal sealed class StubAggregationSource(
    string name,
    string category,
    IReadOnlyList<AggregatedItem> items,
    Exception? throwOnFetch = null,
    TimeSpan delay = default)
    : IAggregationSource
{
    public string Name => name;

    public string Category => category;

    public int CallCount { get; private set; }

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        CallCount++;

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        if (throwOnFetch is not null)
        {
            throw throwOnFetch;
        }

        return items;
    }
}
