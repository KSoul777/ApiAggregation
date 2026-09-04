namespace Aggregator.Aggregation.Application.Feed;

public sealed record SourceOutcome
{
    public required string Source { get; init; }
    public required string Category { get; init; }
    public required bool Succeeded { get; init; }
    public required int ItemCount { get; init; }
    public string? Error { get; init; }
}
