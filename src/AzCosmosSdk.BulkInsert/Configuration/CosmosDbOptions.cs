namespace AzCosmosSdk.BulkInsert.Configuration;

internal sealed record CosmosDbOptions
{
	public const string SectionName = "CosmosDb";

	public string ConnectionString { get; init; } = string.Empty;

	public string DatabaseName { get; init; } = string.Empty;

	public string ContainerName { get; init; } = string.Empty;
}