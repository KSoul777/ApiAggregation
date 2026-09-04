using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.Infrastructure.Sources;

internal sealed class SpaceflightNewsSource(HttpClient httpClient) : IAggregationSource
{
    public const string SourceName = "spaceflight-news";
    public const string SourceCategory = "news";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Name => SourceName;

    public string Category => SourceCategory;

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.MaxItems, 1, 100);
        string url = $"https://api.spaceflightnewsapi.net/v4/articles/?limit={limit}&ordering=-published_at";

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            url += $"&search={Uri.EscapeDataString(request.SearchTerm)}";
        }

        ArticlesResponse? response =
            await httpClient.GetFromJsonAsync<ArticlesResponse>(url, SerializerOptions, cancellationToken);

        if (response?.Results is null)
        {
            return [];
        }

        return response.Results
            .Select(article => new AggregatedItem
            {
                Source = SourceName,
                Category = SourceCategory,
                Title = article.Title,
                Summary = article.Summary,
                Url = article.Url,
                Timestamp = article.PublishedAt,
                Relevance = null,
                Metadata = article.NewsSite is null
                    ? null
                    : new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["newsSite"] = article.NewsSite
                    }
            })
            .ToList();
    }

    private sealed record ArticlesResponse(
        [property: JsonPropertyName("results")] IReadOnlyList<Article>? Results);

    private sealed record Article(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
        [property: JsonPropertyName("news_site")] string? NewsSite);
}
