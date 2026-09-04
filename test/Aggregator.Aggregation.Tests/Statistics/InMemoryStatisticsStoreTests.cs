using Aggregator.Aggregation.Application.Statistics;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Aggregator.Aggregation.Infrastructure.Statistics;
using Aggregator.Aggregation.Tests.TestDoubles;
using AwesomeAssertions;
using Microsoft.Extensions.Options;

namespace Aggregator.Aggregation.Tests.Statistics;

public class InMemoryStatisticsStoreTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static InMemoryStatisticsStore CreateStore(
        FakeDateTimeProvider clock,
        double fast = 100,
        double slow = 200,
        TimeSpan retention = default) =>
        new(clock, Options.Create(new StatisticsOptions
        {
            FastThresholdMs = fast,
            SlowThresholdMs = slow,
            Retention = retention == default ? TimeSpan.FromMinutes(15) : retention
        }));

    [Fact]
    public void Record_TracksTotalsAverageMinMax()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        store.Record("github", 100, success: true);
        store.Record("github", 300, success: true);

        ApiStatisticsSnapshot snapshot = store.GetSnapshot().Single();
        snapshot.Api.Should().Be("github");
        snapshot.TotalRequests.Should().Be(2);
        snapshot.AverageResponseMs.Should().Be(200);
        snapshot.MinResponseMs.Should().Be(100);
        snapshot.MaxResponseMs.Should().Be(300);
        snapshot.LastRequestAt.Should().Be(new DateTimeOffset(Start, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(99, 1, 0, 0)]
    [InlineData(100, 0, 1, 0)]
    [InlineData(200, 0, 1, 0)]
    [InlineData(201, 0, 0, 1)]
    public void Record_BucketsByThreshold(double elapsed, long fast, long average, long slow)
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        store.Record("api", elapsed, success: true);

        PerformanceBuckets buckets = store.GetSnapshot().Single().Buckets;
        buckets.Fast.Should().Be(fast);
        buckets.Average.Should().Be(average);
        buckets.Slow.Should().Be(slow);
    }

    [Fact]
    public void Record_CountsFailures()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        store.Record("api", 50, success: true);
        store.Record("api", 50, success: false);
        store.Record("api", 50, success: false);

        ApiStatisticsSnapshot snapshot = store.GetSnapshot().Single();
        snapshot.TotalRequests.Should().Be(3);
        snapshot.FailedRequests.Should().Be(2);
    }

    [Fact]
    public void GetWindowStatistics_ExcludesSamplesOlderThanWindow()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        store.Record("api", 100, success: true); // t0
        store.Record("api", 100, success: true); // t0

        clock.Advance(TimeSpan.FromMinutes(6));
        store.Record("api", 400, success: true); // t0 + 6m
        store.Record("api", 400, success: true); // t0 + 6m

        ApiWindowStatistics window = store.GetWindowStatistics(TimeSpan.FromMinutes(5)).Single();

        window.SampleCount.Should().Be(2);
        window.AverageResponseMs.Should().Be(400);
    }

    [Fact]
    public void Thresholds_ReflectConfiguredValues()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock, fast: 50, slow: 150);

        store.Thresholds.FastMs.Should().Be(50);
        store.Thresholds.SlowMs.Should().Be(150);
    }

    [Fact]
    public async Task Record_IsThreadSafe_UnderConcurrentWriters()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        const int writers = 8;
        const int perWriter = 2_000;

        await Task.WhenAll(Enumerable.Range(0, writers).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < perWriter; i++)
            {
                store.Record("api", 50, success: true);
            }
        })));

        ApiStatisticsSnapshot snapshot = store.GetSnapshot().Single();
        snapshot.TotalRequests.Should().Be(writers * perWriter);
        snapshot.Buckets.Fast.Should().Be(writers * perWriter);
    }

    [Fact]
    public void GetSnapshot_OrdersApisByName()
    {
        var clock = new FakeDateTimeProvider(Start);
        InMemoryStatisticsStore store = CreateStore(clock);

        store.Record("zeta", 10, success: true);
        store.Record("alpha", 10, success: true);

        store.GetSnapshot().Select(s => s.Api).Should().ContainInOrder("alpha", "zeta");
    }
}
