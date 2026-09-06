using Aggregator.Common.Application.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregator.Common.Infrastructure.Authentication;

internal static class AuthenticationExtensions
{
    private const string EnabledKey = "Authentication:Enabled";

    public static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        bool enabled = configuration.GetValue(EnabledKey, true);

        services.AddAuthorization(authorization =>
            authorization.AddPolicy(
                AuthorizationPolicies.ApiAccess,
                policy =>
                {
                    if (enabled)
                    {
                        policy.RequireAuthenticatedUser();
                    }
                    else
                    {
                        policy.RequireAssertion(_ => true);
                    }
                }));

        if (!enabled)
        {
            return services;
        }

        services.AddAuthentication().AddJwtBearer();
        services.ConfigureOptions<JwtBearerConfigureOptions>();

        return services;
    }
}
