namespace Aggregator.Aggregation.Application.Statistics;

public interface IStatisticsStore
{
    void Record(string apiName, double elapsedMilliseconds, bool success);
    IReadOnlyList<ApiStatisticsSnapshot> GetSnapshot();
    IReadOnlyList<ApiWindowStatistics> GetWindowStatistics(TimeSpan window);
    PerformanceThresholds Thresholds { get; }
}

public sealed record PerformanceThresholds
{
    public required double FastMs { get; init; }

    public required double SlowMs { get; init; }
}
