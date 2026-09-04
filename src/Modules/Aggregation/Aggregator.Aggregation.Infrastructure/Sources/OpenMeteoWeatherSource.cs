using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aggregator.Aggregation.Application.Feed;

namespace Aggregator.Aggregation.Infrastructure.Sources;

internal sealed class OpenMeteoWeatherSource(HttpClient httpClient) : IAggregationSource
{
    public const string SourceName = "open-meteo";
    public const string SourceCategory = "weather";

    private const double DefaultLatitude = 37.9838;
    private const double DefaultLongitude = 23.7275;
    private const string DefaultLocationName = "Athens";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Name => SourceName;

    public string Category => SourceCategory;

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        (double latitude, double longitude, string locationName)? location =
            await ResolveLocationAsync(request, cancellationToken);

        if (location is null)
        {
            return [];
        }

        (double lat, double lon, string name) = location.Value;
        int days = Math.Clamp(request.MaxItems, 1, 16);

        string url = string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
            $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code" +
            $"&forecast_days={days}&timezone=auto");

        ForecastResponse? response =
            await httpClient.GetFromJsonAsync<ForecastResponse>(url, SerializerOptions, cancellationToken);

        DailyForecast? daily = response?.Daily;
        if (daily?.Time is null)
        {
            return [];
        }

        var items = new List<AggregatedItem>(daily.Time.Count);
        for (int i = 0; i < daily.Time.Count; i++)
        {
            double? max = ValueAt(daily.Max, i);
            double? min = ValueAt(daily.Min, i);
            double? precipitation = ValueAt(daily.Precipitation, i);
            int weatherCode = (int?)ValueAt(daily.WeatherCode, i) ?? 0;
            string description = WeatherCodeDescriptions.Describe(weatherCode);

            DateTimeOffset? timestamp =
                DateOnly.TryParse(daily.Time[i], CultureInfo.InvariantCulture, out DateOnly date)
                    ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                    : null;

            items.Add(new AggregatedItem
            {
                Source = SourceName,
                Category = SourceCategory,
                Title = $"{name}: {description}",
                Summary = string.Create(
                    CultureInfo.InvariantCulture,
                    $"Low {min ?? 0:0.#}\u00B0C / High {max ?? 0:0.#}\u00B0C, precipitation {precipitation ?? 0:0.#} mm"),
                Url = null,
                Timestamp = timestamp,
                Relevance = null,
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["location"] = name,
                    ["latitude"] = lat.ToString(CultureInfo.InvariantCulture),
                    ["longitude"] = lon.ToString(CultureInfo.InvariantCulture),
                    ["weatherCode"] = weatherCode.ToString(CultureInfo.InvariantCulture)
                }
            });
        }

        return items;
    }

    private async Task<(double Latitude, double Longitude, string Name)?> ResolveLocationAsync(
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.City))
        {
            return (DefaultLatitude, DefaultLongitude, DefaultLocationName);
        }

        string url = "https://geocoding-api.open-meteo.com/v1/search" +
                     $"?name={Uri.EscapeDataString(request.City)}&count=1&language=en&format=json";

        GeocodingResponse? response = await httpClient.GetFromJsonAsync<GeocodingResponse>(
            url,
            SerializerOptions,
            cancellationToken);

        IReadOnlyList<GeocodingResult>? results = response?.Results;
        if (results is not { Count: > 0 })
        {
            return null;
        }

        GeocodingResult match = results[0];
        return (match.Latitude, match.Longitude, match.Name);
    }

    private static double? ValueAt(IReadOnlyList<double?>? values, int index) =>
        values is not null && index < values.Count ? values[index] : null;

    private sealed record GeocodingResponse(
        [property: JsonPropertyName("results")]
        IReadOnlyList<GeocodingResult>? Results);

    private sealed record GeocodingResult(
        [property: JsonPropertyName("latitude")]
        double Latitude,
        [property: JsonPropertyName("longitude")]
        double Longitude,
        [property: JsonPropertyName("name")] string Name);

    private sealed record ForecastResponse(
        [property: JsonPropertyName("daily")] DailyForecast? Daily);

    private sealed record DailyForecast(
        [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
        [property: JsonPropertyName("temperature_2m_max")]
        IReadOnlyList<double?>? Max,
        [property: JsonPropertyName("temperature_2m_min")]
        IReadOnlyList<double?>? Min,
        [property: JsonPropertyName("precipitation_sum")]
        IReadOnlyList<double?>? Precipitation,
        [property: JsonPropertyName("weather_code")]
        IReadOnlyList<double?>? WeatherCode);
}
