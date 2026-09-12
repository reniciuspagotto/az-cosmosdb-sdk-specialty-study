using AzCosmosSdk.ChangeFeed.Configuration;
using Microsoft.Azure.Cosmos;

namespace AzCosmosSdk.ChangeFeed.Services;

internal static class CosmosClientFactory
{
	public static CosmosClient Create(CosmosDbOptions options)
	{
		var clientOptions = new CosmosClientOptions
		{
			SerializerOptions = new CosmosSerializationOptions
			{
				PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
				IgnoreNullValues = true
			}
		};

		return new CosmosClient(options.ConnectionString, clientOptions);
	}
}