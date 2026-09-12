using AzCosmosSdk.BulkInsert.Configuration;
using AzCosmosSdk.BulkInsert.Services;

const int numberOfProducts = 100;
string[] categories = ["Accessories", "Audio", "Computers", "Gaming", "Office"];

var cosmosOptions = ConfigurationLoader.LoadCosmosOptions();
using var client = CosmosClientFactory.Create(cosmosOptions);

var bulkInsertService = new ProductBulkInsertService(client, cosmosOptions);
var result = await bulkInsertService.InsertProductsAsync(categories, numberOfProducts);

Console.WriteLine($"Bulk insert finished. Success: {result.SuccessCount}. Failure: {result.FailureCount}.");

foreach (var failure in result.Failures)
{
	Console.WriteLine($"Failed to create product {failure.ProductId}: {failure.ErrorMessage}");
}
