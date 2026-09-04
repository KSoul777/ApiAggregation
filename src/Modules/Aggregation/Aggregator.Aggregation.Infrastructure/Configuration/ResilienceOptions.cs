namespace Aggregator.Aggregation.Infrastructure.Configuration;

public sealed class ResilienceOptions
{
    public const string SectionName = "Aggregation:Resilience";
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public double FailureRatio { get; init; } = 0.5;
    public int MinimumThroughput { get; init; } = 10;
    public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(15);
}
