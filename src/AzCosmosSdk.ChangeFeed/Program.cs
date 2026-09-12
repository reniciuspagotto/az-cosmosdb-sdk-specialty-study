using AzCosmosSdk.ChangeFeed.Configuration;
using AzCosmosSdk.ChangeFeed.Models;
using AzCosmosSdk.ChangeFeed.Services;
using Microsoft.Azure.Cosmos;
using static Microsoft.Azure.Cosmos.Container;

const string processorName = "productsProcessor";

var cosmosOptions = ConfigurationLoader.LoadCosmosOptions();
using var client = CosmosClientFactory.Create(cosmosOptions);

await LeaseContainerInitializer.EnsureExistsAsync(client, cosmosOptions);

var sourceContainer = client.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.ContainerName);
var leaseContainer = client.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.LeaseContainerName);

ChangesHandler<Product> handleChanges = async (
	IReadOnlyCollection<Product> changes,
	CancellationToken cancellationToken) =>
{
	Console.WriteLine("START\tHandling batch of changes...");

	foreach (var product in changes)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await Console.Out.WriteLineAsync($"Processing product with name '{product.Name}' and id '{product.Id}'");
	}
};

ChangesEstimationHandler handleEstimation = async (
	long estimation,
	CancellationToken cancellationToken) =>
{
	cancellationToken.ThrowIfCancellationRequested();
	await Console.Out.WriteLineAsync($"ESTIMATE\tRemaining work: {estimation}");
};

var builder = sourceContainer.GetChangeFeedProcessorBuilder<Product>(
	processorName: processorName,
	onChangesDelegate: handleChanges);

var processor = builder
	.WithInstanceName("consoleApp")
	.WithLeaseContainer(leaseContainer)
	.Build();

var estimator = sourceContainer
	.GetChangeFeedEstimatorBuilder(processorName, handleEstimation)
	.WithLeaseContainer(leaseContainer)
	.Build();

await processor.StartAsync();

await estimator.StartAsync();

Console.WriteLine($"RUN\tListening for changes in '{cosmosOptions.ContainerName}'...");
Console.WriteLine($"LEASE\tUsing lease container '{cosmosOptions.LeaseContainerName}'");
Console.WriteLine("Press any key to stop");
Console.ReadKey(intercept: true);

await estimator.StopAsync();
await processor.StopAsync();