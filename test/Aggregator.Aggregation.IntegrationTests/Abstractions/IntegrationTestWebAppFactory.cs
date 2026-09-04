using Aggregator.Aggregation.Application.Feed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Keycloak;
using Testcontainers.Redis;

namespace Aggregator.Aggregation.IntegrationTests.Abstractions;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Realm = "aggregator";
    public const string ClientId = "aggregator-api";
    public const string Username = "demo";
    public const string Password = "demo";

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7.4-alpine").Build();

    private readonly KeycloakContainer _keycloakContainer =
        new KeycloakBuilder("quay.io/keycloak/keycloak:26.1")
            .WithResourceMapping(
                new FileInfo("aggregator-realm-export.json"),
                new FileInfo("/opt/keycloak/data/import/realm.json"))
            .WithCommand("--import-realm")
            .Build();

    public string RealmUrl => $"{_keycloakContainer.GetBaseAddress()}realms/{Realm}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        Environment.SetEnvironmentVariable(
            "ConnectionStrings:Cache", _redisContainer.GetConnectionString());

        Environment.SetEnvironmentVariable("Authentication:Enabled", "true");
        Environment.SetEnvironmentVariable("Authentication:Authority", RealmUrl);
        Environment.SetEnvironmentVariable("Authentication:ValidIssuer", RealmUrl);
        Environment.SetEnvironmentVariable("Authentication:Audience", ClientId);
        Environment.SetEnvironmentVariable("Authentication:RequireHttpsMetadata", "false");

        Environment.SetEnvironmentVariable("Aggregation:Anomaly:Enabled", "false");

        // The app now registers the Keycloak URL health check unconditionally, so point it at a
        // reachable endpoint on the container (the realm's OIDC discovery doc returns 200).
        Environment.SetEnvironmentVariable(
            "KeyCloak:HealthUrl", $"{RealmUrl}/.well-known/openid-configuration");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAggregationSource>();

            services.AddSingleton<IAggregationSource>(new StubAggregationSource(
                "stub-news",
                "news",
                [
                    new AggregatedItem
                    {
                        Source = "stub-news",
                        Category = "news",
                        Title = "Integration news item",
                        Summary = "A stubbed news summary.",
                        Timestamp = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero)
                    }
                ]));

            services.AddSingleton<IAggregationSource>(new StubAggregationSource(
                "stub-repos",
                "repository",
                [
                    new AggregatedItem
                    {
                        Source = "stub-repos",
                        Category = "repository",
                        Title = "Integration repo item",
                        Relevance = 42,
                        Timestamp = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero)
                    }
                ]));
        });
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _keycloakContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        await _keycloakContainer.StopAsync();
    }
}
