using Aggregator.Aggregation.Application.Statistics.GetStatistics;
using Aggregator.Common.Application.Authorization;
using Aggregator.Common.Application.Messaging;
using Aggregator.Common.Domain;
using Aggregator.Common.Presentation.Endpoints;
using Aggregator.Common.Presentation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aggregator.Aggregation.Presentation.Statistics;

internal sealed class GetStatisticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/statistics", HandleAsync)
            .WithName("GetRequestStatistics")
            .WithSummary("Per-API request counts and latency buckets from the in-memory store.")
            .Produces<StatisticsResponse>()
            .RequireAuthorization(AuthorizationPolicies.ApiAccess)
            .WithTags(Tags.Statistics);
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetStatisticsQuery, StatisticsResponse> handler,
        CancellationToken cancellationToken)
    {
        Result<StatisticsResponse> result = await handler.Handle(new GetStatisticsQuery(), cancellationToken);

        return result.Match(Results.Ok, ApiResults.Problem);
    }
}
