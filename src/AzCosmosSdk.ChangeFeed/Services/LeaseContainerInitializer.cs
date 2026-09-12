using AzCosmosSdk.ChangeFeed.Configuration;
using Microsoft.Azure.Cosmos;

namespace AzCosmosSdk.ChangeFeed.Services;

internal static class LeaseContainerInitializer
{
	public static async Task EnsureExistsAsync(CosmosClient client, CosmosDbOptions options)
	{
		var database = client.GetDatabase(options.DatabaseName);
		var containerProperties = new ContainerProperties(options.LeaseContainerName, "/id");

		await database.CreateContainerIfNotExistsAsync(containerProperties);
	}
}