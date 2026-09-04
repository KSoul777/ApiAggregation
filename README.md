# API Aggregator

A .NET 10 / ASP.NET Core service that fans out to several external APIs in parallel,
normalizes their responses into a single feed, and exposes it through one unified,
filterable, sortable endpoint. It ships with resilience + graceful fallback, per‑source
response caching, an in‑memory request‑statistics store, JWT bearer security (Keycloak),
and a background job that flags performance anomalies.

Built on a small Clean‑Architecture / vertical‑slice kernel (custom CQRS + `Result`
pattern, minimal‑API endpoint auto‑discovery, RFC‑7807 errors). Adding a new upstream API
is a one‑class change.

---

## Contents

- [What it does (requirement map)](#what-it-does-requirement-map)
- [Quick start](#quick-start)
- [Endpoints](#endpoints)
- [Filtering, sorting & paging](#filtering-sorting--paging)
- [How it works](#how-it-works)
  - [Sources & parallelism](#sources--parallelism)
  - [Resilience & fallback](#resilience--fallback)
  - [Caching](#caching)
  - [Request statistics](#request-statistics)
  - [Anomaly detection](#anomaly-detection)
  - [Security](#security)
  - [Health checks](#health-checks)
- [Configuration](#configuration)
- [Testing](#testing)
- [Adding a new source](#adding-a-new-source)
- [Project layout](#project-layout)

---

## What it does (requirement map)

| Requirement                                 | Where                                                                                                                                                                                                                                                 |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Aggregate ≥ 3 external APIs concurrently    | [`GetFeedQueryHandler`](src/Modules/Aggregation/Aggregator.Aggregation.Application/Feed/GetFeed/GetFeedQueryHandler.cs) fans out with `Task.WhenAll` over 3 sources                                                                                   |
| Unified endpoint for aggregated data        | `GET /api/feed`                                                                                                                                                                                                                                       |
| Filter & sort by parameters                 | `category`, `source`, `from`/`to`, `sortBy`, `order`, paging — [`FeedQueryProcessor`](src/Modules/Aggregation/Aggregator.Aggregation.Application/Feed/GetFeed/FeedQueryProcessor.cs)                                                                  |
| Transient‑fault handling + fallback         | `Microsoft.Extensions.Http.Resilience` (Polly) per client; per‑source try/catch degrades to partial results                                                                                                                                           |
| Unit + integration tests                    | [`Aggregator.Aggregation.Tests`](test/Aggregator.Aggregation.Tests) (37 unit) · [`Aggregator.Aggregation.IntegrationTests`](test/Aggregator.Aggregation.IntegrationTests) (Testcontainers) · [`ArchitectureTests`](test/Aggregator.ArchitectureTests) |
| Documentation                               | this file + OpenAPI + Swagger UI + Scalar UI + [`requests.http`](requests.http)                                                                                                                                                                       |
| Readiness health checks                     | `GET /health` verifies Redis + Keycloak ([`KeyCloakHealthChecksBuilderExtensions`](src/API/Aggregator.Api/Extensions/KeyCloakHealthChecksBuilderExtensions.cs))                                                                                       |
| Caching                                     | per‑source `ICacheService` decorator over Redis ([`CachingAggregationSource`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Sources/CachingAggregationSource.cs))                                                                     |
| Parallelism                                 | `Task.WhenAll` across sources                                                                                                                                                                                                                         |
| Request statistics (in‑memory, thread‑safe) | `GET /api/statistics` backed by [`InMemoryStatisticsStore`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Statistics/InMemoryStatisticsStore.cs)                                                                                      |
| JWT bearer security                         | Keycloak realm + `AddJwtBearer` ([`AuthenticationExtensions`](src/Common/Aggregator.Common.Infrastructure/Authentication/AuthenticationExtensions.cs))                                                                                                |
| Performance‑anomaly logging                 | Quartz job ([`PerformanceAnomalyJob`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Anomaly/PerformanceAnomalyJob.cs))                                                                                                                |

The three sources are all **keyless** so the service returns real data with zero setup:

| Source             | API                                                               | Category     |
| ------------------ | ----------------------------------------------------------------- | ------------ |
| `open-meteo`       | [Open‑Meteo](https://open-meteo.com) (geocoding + daily forecast) | `weather`    |
| `spaceflight-news` | [Spaceflight News API](https://spaceflightnewsapi.net)            | `news`       |
| `github`           | [GitHub repository search](https://docs.github.com/rest)          | `repository` |

---

## Quick start

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). Docker is only needed for
Keycloak (auth), Seq (logs) and Redis (cache).

### Option A — run without auth or Docker (fastest)

```bash
dotnet run --project src/API/Aggregator.Api --Authentication:Enabled=false --ConnectionStrings:Cache=
```

Blanking `ConnectionStrings:Cache` swaps Redis for an in-process `IDistributedCache`, so nothing
external is required. Drop that flag once `docker compose up -d` is running and the same code
path uses Redis instead.

Then:

```bash
curl "http://localhost:5247/api/feed?city=Berlin&searchTerm=dotnet&pageSize=5"
curl "http://localhost:5247/api/statistics"
```

In `Development` the OpenAPI doc is at `http://localhost:5247/openapi/v1.json` with two
interactive UIs over it: **Swagger UI** at `/swagger` and **Scalar UI** at `/scalar/v1`.
Both show an **Authorize** button (paste a JWT) when auth is enabled.

### Option B — full run with JWT security (Keycloak)

1. Start Keycloak (imports the `aggregator` realm), Seq and Redis:

   ```bash
   docker compose up -d
   ```

   Keycloak: `http://localhost:8080` (admin `admin`/`admin`) · Seq UI: `http://localhost:8081`
   · Redis: `localhost:6379`.

2. Run the API (auth is on by default):

   ```bash
   dotnet run --project src/API/Aggregator.Api
   ```

3. Get a token (password grant; user `demo`/`demo`) and call a protected endpoint:

   ```bash
   TOKEN=$(curl -s -X POST \
     http://localhost:8080/realms/aggregator/protocol/openid-connect/token \
     -d grant_type=password -d client_id=aggregator-api \
     -d username=demo -d password=demo | jq -r .access_token)

   curl -H "Authorization: Bearer $TOKEN" "http://localhost:5247/api/feed?city=Athens"
   ```

`GET /health` is always anonymous (a readiness probe over Redis + Keycloak); `GET /api/feed`
and `GET /api/statistics` require a token when auth is enabled.

> [`requests.http`](requests.http) contains the same calls ready to run from VS Code /
> Visual Studio / Rider.

### Option C — everything in Docker

The API itself is a compose service (built from [its Dockerfile](src/API/Aggregator.Api/Dockerfile)),
so the whole stack — API + Keycloak + Seq + Redis — comes up with one command:

```bash
docker compose up -d --build
```

API at `http://localhost:5247` (auth is off in-container for a friction-free demo — see
`aggregator.api` in [`docker-compose.yml`](docker-compose.yml)). In Visual Studio, the
**docker-compose** project ([`docker-compose.dcproj`](docker-compose.dcproj)) is a launch
target — F5 builds and runs the same stack and opens Scalar.

---

## Endpoints

| Method | Route             | Auth | Description                                                                       |
| ------ | ----------------- | ---- | --------------------------------------------------------------------------------- |
| GET    | `/api/feed`       | ✅   | Aggregated, filtered, sorted, paged feed across all sources                       |
| GET    | `/api/statistics` | ✅   | Per‑API request counts, average latency and performance buckets                   |
| GET    | `/health`         | —    | Readiness probe — aggregates Redis + Keycloak health checks (RFC-compatible JSON) |

### `GET /api/feed` response

```jsonc
{
  "items": [
    {
      "source": "github",
      "category": "repository",
      "title": "dotnet/aspnetcore",
      "summary": "ASP.NET Core is a cross-platform .NET framework ...",
      "url": "https://github.com/dotnet/aspnetcore",
      "timestamp": "2026-09-04T07:12:58+00:00",
      "relevance": 38419,
      "metadata": { "stars": "38419", "forks": "10952", "language": "C#" },
    },
  ],
  "totalCount": 12, // matches after filtering, before paging
  "page": 1,
  "pageSize": 5,
  "sources": [
    // per-source outcome, incl. graceful fallbacks
    {
      "source": "open-meteo",
      "category": "weather",
      "succeeded": true,
      "itemCount": 5,
      "error": null,
    },
    {
      "source": "spaceflight-news",
      "category": "news",
      "succeeded": true,
      "itemCount": 2,
      "error": null,
    },
    {
      "source": "github",
      "category": "repository",
      "succeeded": false,
      "itemCount": 0,
      "error": "...",
    },
  ],
}
```

### `GET /api/statistics` response

```jsonc
{
  "generatedAt": "2026-09-04T07:16:26+00:00",
  "thresholds": { "fastMs": 100, "slowMs": 200 },
  "apis": [
    {
      "api": "github",
      "totalRequests": 12,
      "failedRequests": 0,
      "averageResponseMs": 220.4,
      "minResponseMs": 180.1,
      "maxResponseMs": 540.9,
      "buckets": { "fast": 0, "average": 7, "slow": 5 },
      "lastRequestAt": "2026-09-04T07:16:25+00:00",
    },
  ],
}
```

---

## Filtering, sorting & paging

All parameters are query‑string on `GET /api/feed`.

| Parameter           | Type        | Default      | Notes                                                           |
| ------------------- | ----------- | ------------ | --------------------------------------------------------------- |
| `searchTerm`        | string      | –            | Search term forwarded to text sources (news, GitHub)            |
| `city`              | string      | `Athens`     | Geocoded for weather; falls back to the default location        |
| `maxItemsPerSource` | int (1–100) | 20           | Cap fetched per source before central paging                    |
| `category`          | string      | –            | Filter: `weather` \| `news` \| `repository` (case‑insensitive)  |
| `source`            | string      | –            | Filter by source name (case‑insensitive)                        |
| `from`,`to`         | date‑time   | –            | Filter by item timestamp (inclusive)                            |
| `sortBy`            | enum        | `Timestamp`  | `Timestamp` \| `Relevance` \| `Source` \| `Category` \| `Title` |
| `order`             | enum        | `Descending` | `Ascending` \| `Descending`                                     |
| `page`              | int (≥1)    | 1            |                                                                 |
| `pageSize`          | int (1–100) | 20           |                                                                 |

Examples:

```bash
# Newest news first
curl "http://localhost:5247/api/feed?category=news&sortBy=Timestamp&order=Descending"

# Most-starred repos about kubernetes
curl "http://localhost:5247/api/feed?searchTerm=kubernetes&category=repository&sortBy=Relevance&order=Descending"

# Weather for a place, page 2
curl "http://localhost:5247/api/feed?city=Tokyo&category=weather&page=2&pageSize=3"
```

Invalid parameters return an RFC‑7807 `400` (e.g. `pageSize=0`, `from` after `to`)
via FluentValidation.

---

## How it works

### Sources & parallelism

Every upstream implements a single interface:

```csharp
public interface IAggregationSource
{
    string Name { get; }
    string Category { get; }
    Task<IReadOnlyList<AggregatedItem>> FetchAsync(SourceRequest request, CancellationToken ct);
}
```

The handler injects `IEnumerable<IAggregationSource>` and calls them all with `Task.WhenAll`,
so total latency is roughly the _slowest_ source, not the sum. Each source maps its raw
payload into a canonical [`AggregatedItem`](src/Modules/Aggregation/Aggregator.Aggregation.Application/Feed/AggregatedItem.cs)
(source, category, title, summary, url, timestamp, relevance, metadata) — that single shape
is what makes cross‑source filtering and sorting possible.

### Resilience & fallback

Two independent layers:

- **Transient faults** — each `HttpClient` uses `AddStandardResilienceHandler()` (Polly):
  retries with exponential backoff + jitter, a per‑try and total timeout, and a circuit
  breaker.
- **Fallback** — if a source still fails (or its circuit is open), the handler catches it,
  logs a warning, and returns that source's contribution as **empty** with
  `succeeded: false` and the error in the `sources` array. One dead upstream never fails the
  request; you get partial results plus a clear per‑source status.

### Caching

Each source is wrapped by [`CachingAggregationSource`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Sources/CachingAggregationSource.cs)
(via Scrutor decoration). The decorator only knows
[`ICacheService`](src/Common/Aggregator.Common.Application/Caching/ICacheService.cs) — a small async
abstraction in the Application layer — which
[`CacheService`](src/Common/Aggregator.Common.Infrastructure/Caching/CacheService.cs) implements over
`IDistributedCache`, serializing values as UTF‑8 JSON.

The store is **Redis** whenever `ConnectionStrings:Cache` is set (it is, by default, to
`localhost:6379` — the `redis` service in `docker-compose.yml`); blank it out and the same
`CacheService` runs on an in-process distributed cache instead. Keys are prefixed `aggregator:`
so one Redis can host several apps. Entries always carry an absolute expiration.

Identical requests within the TTL (default 60s) are served from cache — the upstream HTTP
call is skipped entirely, so it also doesn't count against request statistics. Only successful
fetches are cached.

### Request statistics

[`InMemoryStatisticsStore`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Statistics/InMemoryStatisticsStore.cs)
records every outgoing call via a `DelegatingHandler`
([`StatisticsRecordingHandler`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Statistics/StatisticsRecordingHandler.cs))
placed **inside** the resilience pipeline, so each retry counts as a real request with its
own latency. Per API it tracks total/failed counts, average/min/max latency, the last
request time, and counts bucketed as **fast** (`< 100 ms`), **average** (`100–200 ms`) and
**slow** (`> 200 ms`).

**Thread safety:** APIs live in a `ConcurrentDictionary`; each API's counters and its
rolling latency‑sample buffer are mutated under a single per‑API lock, so a snapshot can
never tear (counts, sums and buckets stay consistent). A concurrency test drives 16 000
parallel writes and asserts the totals.

### Anomaly detection

A Quartz job ([`PerformanceAnomalyJob`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Anomaly/PerformanceAnomalyJob.cs))
runs on an interval (default 1 min) and, per API, compares the **last‑5‑minutes** average
latency against the **all‑time baseline**. If the recent window is more than `Factor`×
(default 1.5 ⇒ +50 %) the baseline — and has enough samples — it logs a `Warning`:

```
Performance anomaly for github: last 5m average 640ms is 92% over the all-time baseline 333ms (18 samples).
```

Everything (window, interval, factor, minimum samples) is configurable; the job can be
disabled via `Aggregation:Anomaly:Enabled=false`.

### Security

JWT bearer, validated against a Keycloak realm (`docker compose` imports it from
[`keycloak/realm-export.json`](keycloak/realm-export.json)). Issuer, audience
(`aggregator-api`), lifetime and signature are all validated. `/api/feed` and
`/api/statistics` require an authenticated caller; `/health` is anonymous.

For a frictionless demo, set `Authentication:Enabled=false` — the API‑access policy then
allows anonymous callers and no Keycloak is needed.

Both API UIs expose an **Authorize** button (a bearer scheme injected into the OpenAPI doc by
[`BearerSecuritySchemeTransformer`](src/API/Aggregator.Api/OpenApi/BearerSecuritySchemeTransformer.cs)
when the JWT scheme is registered): paste the token, and it is sent as
`Authorization: Bearer <token>`.

### Health checks

`GET /health` is an ASP.NET Core health-check endpoint (JSON via `UIResponseWriter`) that
aggregates:

- **Redis** — `AddRedis(ConnectionStrings:Cache)`
- **Keycloak** — a URL probe of `KeyCloak:HealthUrl` (Keycloak's management health endpoint,
  port `9000`, enabled by `KC_HEALTH_ENABLED`) via
  [`AddKeyCloak`](src/API/Aggregator.Api/Extensions/KeyCloakHealthChecksBuilderExtensions.cs)

It returns `200 Healthy` only when both are reachable, `503` otherwise — so it doubles as a
container/orchestrator readiness probe.

---

## Configuration

`appsettings.json` (all overridable via environment variables or `--Section:Key=value`):

```jsonc
{
  "ConnectionStrings": { "Cache": "localhost:6379,abortConnect=false" }, 
  "Authentication": {
    "Enabled": true,
    "Authority": "http://localhost:8080/realms/aggregator",
    "Audience": "aggregator-api",
    "ValidIssuer": "http://localhost:8080/realms/aggregator",
    "RequireHttpsMetadata": false,
  },
  "KeyCloak": { "HealthUrl": "http://localhost:9000/health/ready" },
  "Aggregation": {
    "Cache": { "Enabled": true, "Ttl": "00:01:00" },
    "Statistics": {
      "FastThresholdMs": 100,
      "SlowThresholdMs": 200,
      "Retention": "00:15:00",
    },
    "Anomaly": {
      "Enabled": true,
      "Window": "00:05:00",
      "Interval": "00:01:00",
      "Factor": 1.5,
      "MinimumSamples": 5,
    },
    "Sources": { "GitHubToken": "" }, // optional: raises GitHub rate limits
  },
}
```

`ConnectionStrings:Cache` and `KeyCloak:HealthUrl` are read at startup for the health checks —
both must be present (the app fails fast otherwise). The committed `appsettings.json` /
`appsettings.Development.json` already set sensible localhost values.

---

## Testing

```bash
dotnet test Aggregator.slnx            # all tests (integration tests need Docker running)
dotnet test test/Aggregator.Aggregation.Tests   # unit tests only, no Docker
```

- **Unit tests** cover the pure filter/sort/paging logic, the aggregation handler
  (parallel fetch + graceful fallback + per‑source outcomes), the caching decorator
  (hit/miss/disabled/failure‑not‑cached), request validation, and the statistics store
  (buckets, averages, rolling window, and thread safety under load).
- **Integration tests** ([`Aggregator.Aggregation.IntegrationTests`](test/Aggregator.Aggregation.IntegrationTests))
  boot the real API in-memory (`WebApplicationFactory<Program>`) against throwaway **Keycloak**
  and **Redis** containers ([Testcontainers](https://dotnet.testcontainers.org)) — so JWT
  validation (401 without a token, 200 with a real `demo` token) and the Redis-backed health
  check run for real. External HTTP sources are swapped for in-memory stubs. **Requires a
  running Docker daemon.**
- **Architecture tests** (NetArchTest) enforce the layer boundaries (Application/Presentation
  must not depend on Infrastructure; Domain depends on nothing above it).

External APIs are never called in tests — sources are faked, HTTP is not exercised.

---

## Adding a new source

1. Implement `IAggregationSource` in `Aggregator.Aggregation.Infrastructure/Sources` — a typed
   `HttpClient` that fetches and maps to `AggregatedItem`.
2. Register it in [`AggregationModuleConfiguration`](src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/AggregationModuleConfiguration.cs):

   ```csharp
   services.AddHttpClient<MyNewSource>(ConfigureDefaultHeaders)
           .AddResilienceAndStatistics(MyNewSource.SourceName);
   services.AddTransient<IAggregationSource>(sp => sp.GetRequiredService<MyNewSource>());
   ```

That's it — resilience, statistics, caching, parallel fetch, filtering, sorting and paging
all apply automatically.

---

## Project layout

```
src/
  API/Aggregator.Api                     # Composition root: DI, middleware, Serilog, Swagger + Scalar, health checks
  Common/                                # Reusable kernel (no aggregation logic)
    Aggregator.Common.Domain             # Result, Error, ErrorType
    Aggregator.Common.Application         # CQRS interfaces + decorators, auth policy names, clock
    Aggregator.Common.Infrastructure      # DateTimeProvider, Keycloak JWT wiring
    Aggregator.Common.Presentation        # IEndpoint discovery, RFC-7807 ApiResults
  Modules/Aggregation/
    Aggregator.Aggregation.Application     # GetFeed / GetStatistics slices, IAggregationSource, processor
    Aggregator.Aggregation.Infrastructure  # HTTP sources, resilience, caching, stats store, Quartz job
    Aggregator.Aggregation.Presentation    # Minimal-API endpoints
test/
  Aggregator.Aggregation.Tests            # Unit tests
  Aggregator.Aggregation.IntegrationTests # WebApplicationFactory + Testcontainers (Keycloak, Redis)
  Aggregator.ArchitectureTests            # Layer/boundary rules
docker-compose.yml / .override.yml        # API + Keycloak + Seq + Redis
docker-compose.dcproj                     # Visual Studio Docker Compose launch target
```

The solution enforces `TreatWarningsAsErrors`, `AnalysisMode=All`, nullable reference types
and SonarAnalyzer — the build is warning‑clean.
