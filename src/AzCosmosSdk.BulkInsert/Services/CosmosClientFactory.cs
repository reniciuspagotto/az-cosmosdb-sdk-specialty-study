using AzCosmosSdk.BulkInsert.Configuration;
using Microsoft.Azure.Cosmos;

namespace AzCosmosSdk.BulkInsert.Services;

internal static class CosmosClientFactory
{
	public static CosmosClient Create(CosmosDbOptions options)
	{
		var clientOptions = new CosmosClientOptions
		{
			AllowBulkExecution = true,
			SerializerOptions = new CosmosSerializationOptions
			{
				PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
				IgnoreNullValues = true
			}
		};

		return new CosmosClient(options.ConnectionString, clientOptions);
	}
}