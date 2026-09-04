namespace Aggregator.Aggregation.Infrastructure.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Aggregation:Cache";
    public bool Enabled { get; init; } = true;
    public TimeSpan Ttl { get; init; } = TimeSpan.FromSeconds(60);
}
