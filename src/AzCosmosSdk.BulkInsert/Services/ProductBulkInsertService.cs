using AzCosmosSdk.BulkInsert.Configuration;
using AzCosmosSdk.BulkInsert.Models;
using Microsoft.Azure.Cosmos;

namespace AzCosmosSdk.BulkInsert.Services;

internal sealed class ProductBulkInsertService(CosmosClient client, CosmosDbOptions options)
{
	private readonly Container _container = client.GetContainer(options.DatabaseName, options.ContainerName);

	public async Task<BulkInsertResult> InsertProductsAsync(IReadOnlyList<string> categories, int numberOfProducts)
	{
		var products = ProductFactory.Create(categories, numberOfProducts);

		var tasks = products.Select(CreateItemAsync);
		var results = await Task.WhenAll(tasks);

		var failures = results.Where(result => !result.IsSuccess).ToList();

		return new BulkInsertResult(
			SuccessCount: results.Length - failures.Count,
			FailureCount: failures.Count,
			Failures: failures.AsReadOnly());
	}

	private async Task<BulkInsertItemResult> CreateItemAsync(Product product)
	{
		try
		{
			var response = await _container.CreateItemAsync(product, new PartitionKey(product.Category));
			return new BulkInsertItemResult(product.Id, response.StatusCode, null);
		}
		catch (CosmosException exception)
		{
			return new BulkInsertItemResult(product.Id, exception.StatusCode, exception.Message);
		}
	}
}