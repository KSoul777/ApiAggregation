using System.Net;
using System.Net.Http.Json;
using Aggregator.Aggregation.IntegrationTests.Abstractions;
using AwesomeAssertions;

namespace Aggregator.Aggregation.IntegrationTests.Feed;

public sealed class GetFeedEndpointTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/feed", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFeed_WithToken_AggregatesAllStubSources()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        FeedDto? feed = await client.GetFromJsonAsync<FeedDto>(new Uri("/api/feed", UriKind.Relative));

        feed.Should().NotBeNull();
        feed!.TotalCount.Should().Be(2);
        feed.Items.Should().HaveCount(2);
        feed.Sources.Should().OnlyContain(source => source.Succeeded);
        feed.Sources.Should().Contain(source => source.Source == "stub-news");
        feed.Sources.Should().Contain(source => source.Source == "stub-repos");
    }

    [Fact]
    public async Task GetFeed_FilteredByCategory_ReturnsOnlyThatCategory()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        FeedDto? feed = await client.GetFromJsonAsync<FeedDto>(
            new Uri("/api/feed?category=news", UriKind.Relative));

        feed.Should().NotBeNull();
        feed!.Items.Should().ContainSingle();
        feed.Items[0].Category.Should().Be("news");
    }

    private sealed record FeedDto(
        IReadOnlyList<ItemDto> Items,
        int TotalCount,
        IReadOnlyList<SourceDto> Sources);

    private sealed record ItemDto(string Source, string Category, string Title);

    private sealed record SourceDto(string Source, bool Succeeded, int ItemCount);
}
