using Aggregator.Common.Application.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Aggregator.Common.Infrastructure.Authentication;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AuthenticationOptions options =
            configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
            ?? new AuthenticationOptions();

        services.AddAuthorization(authorization =>
            authorization.AddPolicy(
                AuthorizationPolicies.ApiAccess,
                policy =>
                {
                    if (options.Enabled)
                    {
                        policy.RequireAuthenticatedUser();
                    }
                    else
                    {
                        policy.RequireAssertion(_ => true);
                    }
                }));

        if (!options.Enabled)
        {
            return services;
        }

        bool validateAudience = !string.IsNullOrWhiteSpace(options.Audience);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.Audience = options.Audience;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = string.IsNullOrWhiteSpace(options.ValidIssuer)
                        ? options.Authority
                        : options.ValidIssuer,
                    ValidateAudience = validateAudience,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }
}
