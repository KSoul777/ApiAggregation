namespace Aggregator.Aggregation.Application.Feed;

public sealed record SourceRequest
{
    public string? SearchTerm { get; init; }
    public string? City { get; init; }
    public int MaxItems { get; init; } = 20;

    public string CacheKey =>
        string.Join(
            '|',
            (SearchTerm ?? string.Empty).ToLowerInvariant(),
            (City ?? string.Empty).ToLowerInvariant(),
            MaxItems.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
