using Aggregator.Common.Application.Messaging;

namespace Aggregator.Aggregation.Application.Statistics.GetStatistics;

public sealed record GetStatisticsQuery : IQuery<StatisticsResponse>;

public sealed record StatisticsResponse
{
    public required DateTimeOffset GeneratedAt { get; init; }

    public required PerformanceThresholds Thresholds { get; init; }

    public required IReadOnlyList<ApiStatisticsSnapshot> Apis { get; init; }
}
