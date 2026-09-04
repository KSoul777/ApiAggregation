using Aggregator.Common.Application.Caching;
using Aggregator.Common.Application.Clock;
using Aggregator.Common.Infrastructure.Authentication;
using Aggregator.Common.Infrastructure.Caching;
using Aggregator.Common.Infrastructure.Clock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aggregator.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    private const string CacheConnectionStringName = "Cache";

    public static IServiceCollection AddCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthenticationInternal(configuration);

        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        AddCaching(services, configuration);

        return services;
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        string? cacheConnectionString = configuration.GetConnectionString(CacheConnectionStringName);

        if (string.IsNullOrWhiteSpace(cacheConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheConnectionString;
            });
        }

        services.TryAddSingleton<ICacheService, CacheService>();
    }
}
