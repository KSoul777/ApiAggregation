using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.Infrastructure.Sources;

internal sealed class GitHubRepositorySource(HttpClient httpClient) : IAggregationSource
{
    public const string SourceName = "github";
    public const string SourceCategory = "repository";

    private const string DefaultQuery = "stars:>10000";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Name => SourceName;

    public string Category => SourceCategory;

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        int perPage = Math.Clamp(request.MaxItems, 1, 100);
        string query = string.IsNullOrWhiteSpace(request.SearchTerm) ? DefaultQuery : request.SearchTerm;

        string url =
            $"https://api.github.com/search/repositories?q={Uri.EscapeDataString(query)}" +
            $"&sort=stars&order=desc&per_page={perPage}";

        SearchResponse? response =
            await httpClient.GetFromJsonAsync<SearchResponse>(url, SerializerOptions, cancellationToken);

        if (response?.Items is null)
        {
            return [];
        }

        return response.Items
            .Select(repository => new AggregatedItem
            {
                Source = SourceName,
                Category = SourceCategory,
                Title = repository.FullName,
                Summary = repository.Description,
                Url = repository.HtmlUrl,
                Timestamp = repository.UpdatedAt,
                Relevance = repository.Stars,
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["stars"] = repository.Stars.ToString(CultureInfo.InvariantCulture),
                    ["forks"] = repository.Forks.ToString(CultureInfo.InvariantCulture),
                    ["language"] = repository.Language ?? "unknown"
                }
            })
            .ToList();
    }

    private sealed record SearchResponse(
        [property: JsonPropertyName("items")] IReadOnlyList<Repository>? Items);

    private sealed record Repository(
        [property: JsonPropertyName("full_name")] string FullName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("stargazers_count")] long Stars,
        [property: JsonPropertyName("forks_count")] long Forks,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt);
}
