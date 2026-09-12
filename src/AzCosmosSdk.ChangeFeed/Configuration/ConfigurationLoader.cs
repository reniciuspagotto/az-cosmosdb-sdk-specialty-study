using Microsoft.Extensions.Configuration;

namespace AzCosmosSdk.ChangeFeed.Configuration;

internal static class ConfigurationLoader
{
	public static CosmosDbOptions LoadCosmosOptions()
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		var cosmosOptions = configuration.GetSection(CosmosDbOptions.SectionName).Get<CosmosDbOptions>()
			?? throw new InvalidOperationException("CosmosDb configuration is required.");

		if (string.IsNullOrWhiteSpace(cosmosOptions.ConnectionString) ||
			string.IsNullOrWhiteSpace(cosmosOptions.DatabaseName) ||
			string.IsNullOrWhiteSpace(cosmosOptions.ContainerName) ||
			string.IsNullOrWhiteSpace(cosmosOptions.LeaseContainerName))
		{
			throw new InvalidOperationException("CosmosDb connection string, database name, container name, and lease container name are required.");
		}

		return cosmosOptions;
	}
}