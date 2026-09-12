using AzCosmosSdk.Api.Configuration;
using AzCosmosSdk.Api.Endpoints;
using AzCosmosSdk.Api.Handlers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOptions<CosmosDbOptions>()
    .Bind(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "CosmosDb:ConnectionString is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "CosmosDb:DatabaseName is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName), "CosmosDb:ContainerName is required.")
    .ValidateOnStart();

builder.Services.AddSingleton(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CosmosClientConfiguration");
    var cosmosOptions = serviceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

    var endpoint = CosmosConnectionString.ParseAccountEndpoint(cosmosOptions.ConnectionString);

    logger.LogInformation(
        "Configuring Cosmos client for endpoint {Endpoint}, database {DatabaseName}, container {ContainerName}",
        endpoint,
        cosmosOptions.DatabaseName,
        cosmosOptions.ContainerName);

    // Fluent CosmosClientBuilder: alternative to CosmosClientOptions.
    // Each With* method maps to an option (WithConnectionModeDirect/Gateway,
    // WithConsistencyLevel, WithApplicationRegion/WithApplicationPreferredRegions, ...).
    var clientBuilder = new CosmosClientBuilder(cosmosOptions.ConnectionString)
        // Cosmos requires the lowercase "id" property; camelCase keeps the C# model PascalCase.
        .WithSerializerOptions(new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            IgnoreNullValues = true
        })
        // Injects the LogHandler into the SDK's request pipeline so every
        // HTTP request/response "under the hood" is logged.
        .AddCustomHandlers(serviceProvider.GetRequiredService<LogHandler>());

    return clientBuilder.Build();
});

builder.Services.AddSingleton<LogHandler>();

builder.Services.AddSingleton(serviceProvider =>
{
    var cosmosOptions = serviceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    return serviceProvider.GetRequiredService<CosmosClient>()
        .GetContainer(cosmosOptions.DatabaseName, cosmosOptions.ContainerName);
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapProductEndpoints();

app.Run();
