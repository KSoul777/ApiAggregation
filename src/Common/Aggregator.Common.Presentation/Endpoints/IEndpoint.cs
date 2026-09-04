using Microsoft.AspNetCore.Routing;

namespace Aggregator.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
