using System.Net.Http.Headers;
using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Application.Statistics;
using Aggregator.Aggregation.Infrastructure.Anomaly;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Aggregator.Aggregation.Infrastructure.Sources;
using Aggregator.Aggregation.Infrastructure.Statistics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Quartz;

namespace Aggregator.Aggregation.Infrastructure;

public static class AggregationModuleConfiguration
{
    private const string UserAgent = "aggregator-service/1.0";

    public static IServiceCollection AddAggregationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StatisticsOptions>(configuration.GetSection(StatisticsOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.Configure<AnomalyOptions>(configuration.GetSection(AnomalyOptions.SectionName));

        services.AddSingleton<IStatisticsStore, InMemoryStatisticsStore>();

        AddSources(services, configuration);

        services.Decorate<IAggregationSource, CachingAggregationSource>();

        AddAnomalyDetection(services, configuration);

        return services;
    }

    private static void AddSources(IServiceCollection services, IConfiguration configuration)
    {
        SourceOptions sourceOptions =
            configuration.GetSection(SourceOptions.SectionName).Get<SourceOptions>() ?? new SourceOptions();

        services
            .AddHttpClient<OpenMeteoWeatherSource>(ConfigureDefaultHeaders)
            .AddResilienceAndStatistics(OpenMeteoWeatherSource.SourceName);
        services.AddTransient<IAggregationSource>(sp => sp.GetRequiredService<OpenMeteoWeatherSource>());

        services
            .AddHttpClient<SpaceflightNewsSource>(ConfigureDefaultHeaders)
            .AddResilienceAndStatistics(SpaceflightNewsSource.SourceName);
        services.AddTransient<IAggregationSource>(sp => sp.GetRequiredService<SpaceflightNewsSource>());

        services
            .AddHttpClient<GitHubRepositorySource>(client =>
            {
                ConfigureDefaultHeaders(client);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                if (!string.IsNullOrWhiteSpace(sourceOptions.GitHubToken))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", sourceOptions.GitHubToken);
                }
            })
            .AddResilienceAndStatistics(GitHubRepositorySource.SourceName);
        services.AddTransient<IAggregationSource>(sp => sp.GetRequiredService<GitHubRepositorySource>());
    }

    private static void ConfigureDefaultHeaders(HttpClient client) =>
        client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse(UserAgent));

    private static void AddResilienceAndStatistics(this IHttpClientBuilder builder, string apiName)
    {
        builder.AddResilienceHandler(
            "aggregation-" + apiName,
            static (pipeline, context) =>
            {
                ResilienceOptions options = context.ServiceProvider
                    .GetRequiredService<IOptions<ResilienceOptions>>().Value;

                pipeline
                    .AddTimeout(options.TotalTimeout)
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.MaxRetryAttempts,
                        Delay = options.BaseDelay,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.FailureRatio,
                        MinimumThroughput = options.MinimumThroughput,
                        SamplingDuration = options.SamplingDuration,
                        BreakDuration = options.BreakDuration,
                    })
                    .AddTimeout(options.AttemptTimeout);
            });

        builder.AddHttpMessageHandler(sp => new StatisticsRecordingHandler(apiName, sp.GetRequiredService<IStatisticsStore>()));
    }

    private static void AddAnomalyDetection(IServiceCollection services, IConfiguration configuration)
    {
        AnomalyOptions anomalyOptions =
            configuration.GetSection(AnomalyOptions.SectionName).Get<AnomalyOptions>() ?? new AnomalyOptions();

        if (!anomalyOptions.Enabled)
        {
            return;
        }

        services.AddQuartz(quartz =>
        {
            quartz.AddJob<PerformanceAnomalyJob>(job => job.WithIdentity(PerformanceAnomalyJob.Key));
            quartz.AddTrigger(trigger => trigger
                .ForJob(PerformanceAnomalyJob.Key)
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(anomalyOptions.Interval)
                    .RepeatForever())
                .StartAt(DateBuilder.FutureDate(
                    (int)anomalyOptions.Interval.TotalSeconds,
                    IntervalUnit.Second)));
        });

        services.AddQuartzHostedService(quartz => quartz.WaitForJobsToComplete = true);
    }
}
