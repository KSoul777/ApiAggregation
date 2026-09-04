using System.Buffers;
using System.Text.Json;
using Aggregator.Aggregation.Application.Feed;
using AwesomeAssertions;

namespace Aggregator.Aggregation.Tests.Feed;

/// <summary>
/// The cache is distributed (Redis), so cached feed payloads make a UTF-8 JSON round-trip.
/// These pin that contract: a change to <see cref="AggregatedItem"/> that survives compilation
/// but not serialization would otherwise only show up as silent cache misses at runtime.
/// Mirrors the serializer settings <c>CacheService</c> uses - defaults, no options object.
/// </summary>
public class AggregatedItemSerializationTests
{
    [Fact]
    public void FullyPopulatedItem_SurvivesRoundTrip()
    {
        var original = new AggregatedItem
        {
            Source = "github",
            Category = "repository",
            Title = "dotnet/runtime",
            Summary = "The .NET runtime",
            Url = "https://github.com/dotnet/runtime",
            Timestamp = new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.FromHours(2)),
            Relevance = 16234,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = "C#",
                ["forks"] = "4821"
            }
        };

        AggregatedItem? restored = RoundTrip(original);

        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void ItemWithOnlyRequiredFields_SurvivesRoundTrip()
    {
        var original = new AggregatedItem
        {
            Source = "open-meteo",
            Category = "weather",
            Title = "Athens"
        };

        AggregatedItem? restored = RoundTrip(original);

        restored.Should().BeEquivalentTo(original);
        restored!.Metadata.Should().BeNull();
    }

    [Fact]
    public void ItemList_RoundTripsThroughTheInterfaceTypeTheCacheStores()
    {
        IReadOnlyList<AggregatedItem> original =
        [
            new AggregatedItem { Source = "a", Category = "news", Title = "first" },
            new AggregatedItem { Source = "b", Category = "weather", Title = "second", Relevance = 1.5 }
        ];

        IReadOnlyList<AggregatedItem>? restored = RoundTrip(original);

        restored.Should().BeEquivalentTo(original, options => options.WithStrictOrdering());
    }

    private static T? RoundTrip<T>(T value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            JsonSerializer.Serialize(writer, value);
        }

        return JsonSerializer.Deserialize<T>(buffer.WrittenSpan);
    }
}
