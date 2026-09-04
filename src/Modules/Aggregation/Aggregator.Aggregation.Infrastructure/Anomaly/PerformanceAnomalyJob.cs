using Aggregator.Aggregation.Application.Statistics;
using Aggregator.Aggregation.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Aggregator.Aggregation.Infrastructure.Anomaly;

[DisallowConcurrentExecution]
internal sealed class PerformanceAnomalyJob(
    IStatisticsStore statisticsStore,
    IOptions<AnomalyOptions> options,
    ILogger<PerformanceAnomalyJob> logger)
    : IJob
{
    public static readonly JobKey Key = new("performance-anomaly");

    public Task Execute(IJobExecutionContext context)
    {
        AnomalyOptions anomalyOptions = options.Value;

        IReadOnlyList<ApiStatisticsSnapshot> snapshots = statisticsStore.GetSnapshot();
        var windows = statisticsStore
            .GetWindowStatistics(anomalyOptions.Window)
            .ToDictionary(window => window.Api, StringComparer.OrdinalIgnoreCase);

        foreach (ApiStatisticsSnapshot snapshot in snapshots)
        {
            if (!windows.TryGetValue(snapshot.Api, out ApiWindowStatistics? window))
            {
                continue;
            }

            if (window.SampleCount < anomalyOptions.MinimumSamples)
            {
                continue;
            }

            double baseline = snapshot.AverageResponseMs;
            if (baseline <= 0)
            {
                continue;
            }

            if (window.AverageResponseMs <= baseline * anomalyOptions.Factor)
            {
                continue;
            }

            double percentOver = (window.AverageResponseMs / baseline - 1) * 100;

            logger.LogWarning(
                "Performance anomaly for {Api}: last {WindowMinutes:0.#}m average {WindowMs:0}ms is " +
                "{PercentOver:0}% over the all-time baseline {BaselineMs:0}ms ({SampleCount} samples).",
                snapshot.Api,
                anomalyOptions.Window.TotalMinutes,
                window.AverageResponseMs,
                percentOver,
                baseline,
                window.SampleCount);
        }

        return Task.CompletedTask;
    }
}
