namespace Aggregator.Aggregation.Application.Statistics;

public sealed record ApiStatisticsSnapshot
{
    public required string Api { get; init; }
    public required long TotalRequests { get; init; }
    public required long FailedRequests { get; init; }
    public required double AverageResponseMs { get; init; }
    public required double MinResponseMs { get; init; }
    public required double MaxResponseMs { get; init; }
    public required PerformanceBuckets Buckets { get; init; }
    public DateTimeOffset? LastRequestAt { get; init; }
}

public sealed record PerformanceBuckets
{
    public required long Fast { get; init; }
    public required long Average { get; init; }
    public required long Slow { get; init; }
}

public sealed record ApiWindowStatistics
{
    public required string Api { get; init; }
    public required int SampleCount { get; init; }
    public required double AverageResponseMs { get; init; }
}
