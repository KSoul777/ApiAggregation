namespace Aggregator.Aggregation.Infrastructure.Configuration;

public sealed class SourceOptions
{
    public const string SectionName = "Aggregation:Sources";
    public string? GitHubToken { get; init; }
}
