using Aggregator.Aggregation.Application.Feed;
using Aggregator.Aggregation.Application.Feed.GetFeed;
using Aggregator.Common.Application.Authorization;
using Aggregator.Common.Application.Messaging;
using Aggregator.Common.Domain;
using Aggregator.Common.Presentation.Endpoints;
using Aggregator.Common.Presentation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aggregator.Aggregation.Presentation.Feed;

internal sealed class GetFeedEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feed", HandleAsync)
            .WithName("GetAggregatedFeed")
            .WithSummary("Returns the aggregated feed across all external sources.")
            .Produces<FeedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthorizationPolicies.ApiAccess)
            .WithTags(Tags.Aggregation);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] Request request,
        IQueryHandler<GetFeedQuery, FeedResponse> handler,
        CancellationToken cancellationToken)
    {
        Result<FeedResponse> result = await handler.Handle(request.ToQuery(), cancellationToken);

        return result.Match(Results.Ok, ApiResults.Problem);
    }

    private sealed record Request(
        string? SearchTerm,
        string? City,
        string? Category,
        string? Source,
        DateTimeOffset? From,
        DateTimeOffset? To,
        FeedSortField? SortBy,
        SortDirection? Order,
        int? Page,
        int? PageSize,
        int? MaxItemsPerSource)
    {
        public GetFeedQuery ToQuery() =>
            new()
            {
                SearchTerm = SearchTerm,
                City = City,
                Category = Category,
                Source = Source,
                From = From,
                To = To,
                SortBy = SortBy ?? FeedSortField.Timestamp,
                Order = Order ?? SortDirection.Descending,
                Page = Page ?? 1,
                PageSize = PageSize ?? 20,
                MaxItemsPerSource = MaxItemsPerSource ?? 20
            };
    }
}
