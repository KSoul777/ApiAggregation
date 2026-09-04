using System.Diagnostics;
using Aggregator.Aggregation.Application.Statistics;

namespace Aggregator.Aggregation.Infrastructure.Statistics;

internal sealed class StatisticsRecordingHandler(string apiName, IStatisticsStore statisticsStore)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        bool success = false;

        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            success = response.IsSuccessStatusCode;
            return response;
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            statisticsStore.Record(apiName, elapsedMs, success);
        }
    }
}
