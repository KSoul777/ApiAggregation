namespace Aggregator.Common.Infrastructure.Authentication;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    public bool Enabled { get; init; } = true;
    public string Authority { get; init; } = "http://localhost:8080/realms/aggregator";
    public string? Audience { get; init; } = "aggregator-api";
    public string? ValidIssuer { get; init; }
    public bool RequireHttpsMetadata { get; init; }
}
