using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Aggregator.Common.Application.Caching;
using Microsoft.Extensions.Options;

namespace Aggregator.Aggregation.Infrastructure.Sources;

internal sealed class CachingAggregationSource(
    IAggregationSource inner,
    ICacheService cache,
    IOptions<CacheOptions> options)
    : IAggregationSource
{
    public string Name => inner.Name;

    public string Category => inner.Category;

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        CacheOptions cacheOptions = options.Value;

        if (!cacheOptions.Enabled)
        {
            return await inner.FetchAsync(request, cancellationToken);
        }

        string cacheKey = $"source:{inner.Name}:{request.CacheKey}";

        IReadOnlyList<AggregatedItem>? cached =
            await cache.GetAsync<IReadOnlyList<AggregatedItem>>(cacheKey, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        IReadOnlyList<AggregatedItem> items = await inner.FetchAsync(request, cancellationToken);

        await cache.SetAsync(cacheKey, items, cacheOptions.Ttl, cancellationToken);

        return items;
    }
}
