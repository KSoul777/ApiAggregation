using Aggregator.Common.Application.Clock;
using Aggregator.Common.Application.Messaging;
using Aggregator.Common.Domain;

namespace Aggregator.Aggregation.Application.Statistics.GetStatistics;

internal sealed class GetStatisticsQueryHandler(
    IStatisticsStore statisticsStore,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetStatisticsQuery, StatisticsResponse>
{
    public Task<Result<StatisticsResponse>> Handle(
        GetStatisticsQuery query,
        CancellationToken cancellationToken)
    {
        var response = new StatisticsResponse
        {
            GeneratedAt = dateTimeProvider.UtcNow,
            Thresholds = statisticsStore.Thresholds,
            Apis = statisticsStore.GetSnapshot()
        };

        return Task.FromResult<Result<StatisticsResponse>>(response);
    }
}
