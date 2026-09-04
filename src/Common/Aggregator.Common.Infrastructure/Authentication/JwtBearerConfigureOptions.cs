using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aggregator.Common.Infrastructure.Authentication;

/// <summary>
/// Binds the "Authentication" configuration section directly onto <see cref="JwtBearerOptions"/>
/// (Authority, Audience, RequireHttpsMetadata, TokenValidationParameters, ...). Same approach as
/// the Evently reference project - config is the single source of truth for JWT validation.
/// </summary>
internal sealed class JwtBearerConfigureOptions(IConfiguration configuration)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    private const string ConfigurationSectionName = "Authentication";

    public void Configure(JwtBearerOptions options) =>
        configuration.GetSection(ConfigurationSectionName).Bind(options);

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);
}
