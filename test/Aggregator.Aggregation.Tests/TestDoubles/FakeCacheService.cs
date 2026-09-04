using Aggregator.Common.Application.Caching;

namespace Aggregator.Aggregation.Tests.TestDoubles;

/// <summary>
/// Dictionary-backed <see cref="ICacheService"/> with no expiration, which is all the decorator
/// tests need - they assert hit/miss and key isolation, not eviction timing.
/// </summary>
internal sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

    public int SetCount { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.TryGetValue(key, out object? value) && value is T typed ? typed : default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        _entries[key] = value;
        SetCount++;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.Remove(key);

        return Task.CompletedTask;
    }
}
