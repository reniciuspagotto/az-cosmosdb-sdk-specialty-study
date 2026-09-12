namespace AzCosmosSdk.Api.Configuration;

public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
}
