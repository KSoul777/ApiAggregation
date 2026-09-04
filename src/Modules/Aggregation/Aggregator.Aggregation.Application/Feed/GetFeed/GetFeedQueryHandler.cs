using Aggregator.Common.Application.Messaging;
using Aggregator.Common.Domain;
using Microsoft.Extensions.Logging;

namespace Aggregator.Aggregation.Application.Feed.GetFeed;

internal sealed class GetFeedQueryHandler(
    IEnumerable<IAggregationSource> sources,
    ILogger<GetFeedQueryHandler> logger)
    : IQueryHandler<GetFeedQuery, FeedResponse>
{
    public async Task<Result<FeedResponse>> Handle(GetFeedQuery query, CancellationToken cancellationToken)
    {
        var request = query.ToSourceRequest();

        SourceFetch[] fetches = await Task.WhenAll(
            sources.Select(source => FetchSafeAsync(source, request, cancellationToken)));

        (IReadOnlyList<AggregatedItem> items, int totalCount) =
            FeedQueryProcessor.Apply(fetches.SelectMany(f => f.Items), query);

        var response = new FeedResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Sources = fetches.Select(f => f.Outcome).ToList()
        };

        return response;
    }

    private async Task<SourceFetch> FetchSafeAsync(
        IAggregationSource source,
        SourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<AggregatedItem> items = await source.FetchAsync(request, cancellationToken);

            return new SourceFetch(
                items,
                new SourceOutcome
                {
                    Source = source.Name,
                    Category = source.Category,
                    Succeeded = true,
                    ItemCount = items.Count
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Source {Source} failed; falling back to no items for this request",
                source.Name);

            return new SourceFetch(
                [],
                new SourceOutcome
                {
                    Source = source.Name,
                    Category = source.Category,
                    Succeeded = false,
                    ItemCount = 0,
                    Error = exception.Message
                });
        }
    }

    private sealed record SourceFetch(IReadOnlyList<AggregatedItem> Items, SourceOutcome Outcome);
}
