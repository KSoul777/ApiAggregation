using System.Collections.Concurrent;
using Aggregator.Aggregation.Application.Statistics;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Aggregator.Common.Application.Clock;
using Microsoft.Extensions.Options;

namespace Aggregator.Aggregation.Infrastructure.Statistics;

internal sealed class InMemoryStatisticsStore(
    IDateTimeProvider dateTimeProvider,
    IOptions<StatisticsOptions> options)
    : IStatisticsStore
{
    private readonly ConcurrentDictionary<string, ApiStat> _stats = new(StringComparer.OrdinalIgnoreCase);
    private readonly StatisticsOptions _options = options.Value;

    public PerformanceThresholds Thresholds => new()
    {
        FastMs = _options.FastThresholdMs,
        SlowMs = _options.SlowThresholdMs
    };

    public void Record(string apiName, double elapsedMilliseconds, bool success)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);

        ApiStat stat = _stats.GetOrAdd(apiName, static _ => new ApiStat());
        stat.Record(
            elapsedMilliseconds,
            success,
            dateTimeProvider.UtcNow,
            _options.FastThresholdMs,
            _options.SlowThresholdMs,
            _options.Retention);
    }

    public IReadOnlyList<ApiStatisticsSnapshot> GetSnapshot() =>
        _stats
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value.ToSnapshot(pair.Key))
            .ToList();

    public IReadOnlyList<ApiWindowStatistics> GetWindowStatistics(TimeSpan window)
    {
        DateTime cutoff = dateTimeProvider.UtcNow - window;

        return _stats
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value.ToWindow(pair.Key, cutoff))
            .ToList();
    }

    private sealed class ApiStat
    {
        private readonly Lock _gate = new();
        private readonly Queue<Sample> _samples = new();

        private long _total;
        private long _failed;
        private long _fast;
        private long _average;
        private long _slow;
        private double _sum;
        private double _min = double.MaxValue;
        private double _max;
        private DateTime? _lastRequestUtc;

        public void Record(
            double elapsedMs,
            bool success,
            DateTime nowUtc,
            double fastThresholdMs,
            double slowThresholdMs,
            TimeSpan retention)
        {
            double value = elapsedMs < 0 ? 0 : elapsedMs;

            lock (_gate)
            {
                _total++;
                if (!success)
                {
                    _failed++;
                }

                _sum += value;
                _min = Math.Min(_min, value);
                _max = Math.Max(_max, value);
                _lastRequestUtc = nowUtc;

                if (value < fastThresholdMs)
                {
                    _fast++;
                }
                else if (value > slowThresholdMs)
                {
                    _slow++;
                }
                else
                {
                    _average++;
                }

                _samples.Enqueue(new Sample(nowUtc, value));
                Prune(nowUtc - retention);
            }
        }

        public ApiStatisticsSnapshot ToSnapshot(string api)
        {
            lock (_gate)
            {
                return new ApiStatisticsSnapshot
                {
                    Api = api,
                    TotalRequests = _total,
                    FailedRequests = _failed,
                    AverageResponseMs = _total == 0 ? 0 : _sum / _total,
                    MinResponseMs = _total == 0 ? 0 : _min,
                    MaxResponseMs = _max,
                    Buckets = new PerformanceBuckets
                    {
                        Fast = _fast,
                        Average = _average,
                        Slow = _slow
                    },
                    LastRequestAt = _lastRequestUtc is null
                        ? null
                        : new DateTimeOffset(_lastRequestUtc.Value, TimeSpan.Zero)
                };
            }
        }

        public ApiWindowStatistics ToWindow(string api, DateTime cutoffUtc)
        {
            lock (_gate)
            {
                int count = 0;
                double sum = 0;
                foreach (Sample sample in _samples.Where(sample => sample.TimestampUtc > cutoffUtc))
                {
                    count++;
                    sum += sample.ElapsedMs;
                }

                return new ApiWindowStatistics
                {
                    Api = api,
                    SampleCount = count,
                    AverageResponseMs = count == 0 ? 0 : sum / count
                };
            }
        }

        private void Prune(DateTime cutoffUtc)
        {
            while (_samples.TryPeek(out Sample oldest) && oldest.TimestampUtc <= cutoffUtc)
            {
                _samples.Dequeue();
            }
        }

        private readonly record struct Sample(DateTime TimestampUtc, double ElapsedMs);
    }
}
