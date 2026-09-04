namespace Aggregator.Aggregation.Infrastructure.Configuration;

public sealed class AnomalyOptions
{
    public const string SectionName = "Aggregation:Anomaly";
    public bool Enabled { get; init; } = true;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);
    public double Factor { get; init; } = 1.5;
    public int MinimumSamples { get; init; } = 5;
}
