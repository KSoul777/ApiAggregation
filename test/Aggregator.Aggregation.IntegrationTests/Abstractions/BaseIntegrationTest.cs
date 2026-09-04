using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Aggregator.Aggregation.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest(IntegrationTestWebAppFactory factory)
{
    protected IntegrationTestWebAppFactory Factory => factory;

    protected HttpClient CreateClient() => factory.CreateClient();

    protected async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        HttpClient client = factory.CreateClient();
        string token = await GetAccessTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var tokenClient = new HttpClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = IntegrationTestWebAppFactory.ClientId,
            ["username"] = IntegrationTestWebAppFactory.Username,
            ["password"] = IntegrationTestWebAppFactory.Password
        });

        HttpResponseMessage response = await tokenClient.PostAsync(
            new Uri($"{factory.RealmUrl}/protocol/openid-connect/token"),
            content);

        response.EnsureSuccessStatusCode();

        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        return token?.AccessToken ?? throw new InvalidOperationException("No access token returned.");
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken);
}
