using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aggregator.Common.Infrastructure.Authentication;

internal sealed class JwtBearerConfigureOptions(IConfiguration configuration)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    private const string ConfigurationSectionName = "Authentication";
    private const string ValidIssuerKey = "Authentication:ValidIssuer";

    public void Configure(JwtBearerOptions options)
    {
        configuration.GetSection(ConfigurationSectionName).Bind(options);

        string? validIssuer = configuration.GetValue<string>(ValidIssuerKey);

        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidIssuer = string.IsNullOrWhiteSpace(validIssuer) ? options.Authority : validIssuer;
        options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(options.Audience);
        options.TokenValidationParameters.ValidAudience = options.Audience;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
    }

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);
}
