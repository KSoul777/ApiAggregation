using System.Globalization;
using System.Reflection;
using Aggregator.Aggregation.Infrastructure;
using Aggregator.Api.Extensions;
using Aggregator.Api.Middleware;
using Aggregator.Api.OpenApi;
using Aggregator.Common.Application;
using Aggregator.Common.Infrastructure;
using Aggregator.Common.Infrastructure.Configuration;
using Aggregator.Common.Presentation.Endpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);



builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddSwaggerDocumentation();

Assembly[] moduleApplicationAssemblies =
[
    Aggregator.Aggregation.Application.AssemblyReference.Assembly
];

builder.Services.AddApplication(moduleApplicationAssemblies);
builder.Services.AddCommonInfrastructure(builder.Configuration);
builder.Services.AddAggregationModule(builder.Configuration);
builder.Services.AddEndpoints(Aggregator.Aggregation.Presentation.AssemblyReference.Assembly);

string redisConnectionString = builder.Configuration.GetConnectionStringOrThrow("Cache");
Uri keyCloakHealthUrl = builder.Configuration.GetKeyCloakHealthUrl();

builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString)
    .AddKeyCloak(keyCloakHealthUrl);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Aggregator API"));
}

app.UseLogContextTraceLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapEndpoints();

await app.RunAsync();

public partial class Program;
