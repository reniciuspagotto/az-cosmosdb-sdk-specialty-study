using Bogus;
using AzCosmosSdk.BulkInsert.Models;

namespace AzCosmosSdk.BulkInsert.Services;

internal static class ProductFactory
{
	public static IReadOnlyList<Product> Create(IReadOnlyList<string> categories, int numberOfProducts)
	{
		if (categories.Count == 0)
		{
			throw new ArgumentException("At least one category must be provided.", nameof(categories));
		}

		var tagPool = new[]
		{
			"bulk",
			"sdk",
			"console",
			"lab",
			"training",
			"cosmos"
		};

		var faker = new Faker<Product>()
			.CustomInstantiator(fake => new Product(
				Id: Guid.CreateVersion7().ToString(),
				Name: fake.Commerce.ProductName(),
				Category: fake.PickRandom(categories.ToArray()),
				Price: Math.Round(fake.Random.Decimal(10, 500), 2),
				Quantity: fake.Random.Int(1, 250),
				Tags: fake.PickRandom(tagPool, fake.Random.Int(2, 4)).ToList()));

		return Enumerable.Range(1, numberOfProducts)
			.Select(_ => faker.Generate())
			.ToList()
			.AsReadOnly();
	}
}