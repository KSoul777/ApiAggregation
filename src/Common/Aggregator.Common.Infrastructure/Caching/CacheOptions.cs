using Microsoft.Extensions.Caching.Distributed;

namespace Aggregator.Common.Infrastructure.Caching;

internal static class CacheOptions
{
    private static readonly DistributedCacheEntryOptions DefaultExpiration = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    internal static DistributedCacheEntryOptions Create(TimeSpan? expiration) =>
        expiration is null
            ? DefaultExpiration
            : new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
}
