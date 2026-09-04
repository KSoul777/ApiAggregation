using System.Net;
using Aggregator.Aggregation.IntegrationTests.Abstractions;
using AwesomeAssertions;

namespace Aggregator.Aggregation.IntegrationTests.System;

public sealed class SystemEndpointTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Health_ReportsHealthy_WhenRedisIsUp()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetStatistics_WithToken_ReturnsOk()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/statistics", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatistics_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/statistics", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
