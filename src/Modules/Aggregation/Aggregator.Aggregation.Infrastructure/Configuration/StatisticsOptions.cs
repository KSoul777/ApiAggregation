namespace Aggregator.Aggregation.Infrastructure.Configuration;

public sealed class StatisticsOptions
{
    public const string SectionName = "Aggregation:Statistics";
    public double FastThresholdMs { get; init; } = 100;
    public double SlowThresholdMs { get; init; } = 200;
    public TimeSpan Retention { get; init; } = TimeSpan.FromMinutes(15);
}
