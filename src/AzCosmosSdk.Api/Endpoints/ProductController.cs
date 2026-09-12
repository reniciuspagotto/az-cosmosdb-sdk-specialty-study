using AzCosmosSdk.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;

namespace AzCosmosSdk.Api.Endpoints;

public static class ProductController
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/products");

        group.MapGet("/", GetProductsAsync)
            .WithName("GetProducts");

        group.MapGet("/{id}", GetProductByIdAsync)
            .WithName("GetProductById");

        group.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct");

        group.MapPost("/batch", CreateProductsBatchAsync)
            .WithName("CreateProductsBatch");

        group.MapPost("/batch/update-price", UpdateProductsPriceBatchAsync)
            .WithName("UpdateProductsPriceBatch");

        group.MapPut("/{id}", UpdateProductAsync)
            .WithName("UpdateProduct");

        group.MapDelete("/{id}", DeleteProductAsync)
            .WithName("DeleteProductLogically");

        group.MapDelete("/{id}/hard", DeleteProductPermanentlyAsync)
            .WithName("DeleteProductPermanently");

        group.MapDelete("/partition/{category}", DeleteProductsByPartitionAsync)
            .WithName("DeleteProductsByPartition");

        group.MapPost("/{id}/expire", ExpireProductAsync)
            .WithName("ExpireProduct");

        return routes;
    }

    private static async Task<IResult> GetProductsAsync(string? continuationToken, Container container)
    {
        var query = container.GetItemLinqQueryable<Product>()
            .ToQueryDefinition();

        using var iterator = container.GetItemQueryIterator<Product>(
            queryDefinition: query,
            continuationToken: continuationToken,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = DefaultPageSize
            });

        var response = await iterator.ReadNextAsync();

        return Results.Ok(new ProductListResponse(response.Resource.ToList().AsReadOnly(), response.ContinuationToken));
    }

    private static async Task<IResult> GetProductByIdAsync(string id, string category, Container container)
    {
        try
        {
            var response = await container.ReadItemAsync<Product>(id, new PartitionKey(category));

            if (response.Resource.DeletedAt is not null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response.Resource);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CreateProductAsync(CreateProductRequest request, Container container)
    {
        var product = new Product(
            Id: Guid.CreateVersion7().ToString(),
            Name: request.Name,
            Category: request.Category,
            Price: request.Price,
            Quantity: request.Quantity,
            Tags: request.Tags);

        var response = await container.CreateItemAsync(product, new PartitionKey(product.Category));

        return Results.Created($"/products/{response.Resource.Id}?category={Uri.EscapeDataString(response.Resource.Category)}", response.Resource);
    }

    private static async Task<IResult> CreateProductsBatchAsync(CreateProductsBatchRequest request, Container container)
    {
        if (request.Products.Count == 0)
        {
            return Results.BadRequest("At least one product must be provided.");
        }

        var category = request.Category?.Trim();

        if (string.IsNullOrWhiteSpace(category))
        {
            return Results.BadRequest("Category is required.");
        }

        var products = request.Products
            .Select(product => new Product(
                Id: Guid.CreateVersion7().ToString(),
                Name: product.Name,
                Category: category,
                Price: product.Price,
                Quantity: product.Quantity,
                Tags: product.Tags))
            .ToList();

        var batch = container.CreateTransactionalBatch(new PartitionKey(category));

        foreach (var product in products)
        {
            batch.CreateItem(product);
        }

        var batchResponse = await batch.ExecuteAsync();

        if (!batchResponse.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)batchResponse.StatusCode);
        }

        return Results.Created($"/products/partition/{Uri.EscapeDataString(category)}", new CreateProductsBatchResponse(category, products.Count, products.AsReadOnly()));
    }

    private static async Task<IResult> UpdateProductsPriceBatchAsync(BatchUpdatePriceRequest request, Container container)
    {
        if (request.Items.Count == 0)
        {
            return Results.BadRequest("At least one product must be provided.");
        }

        var updatedCount = 0;
        var categories = new List<string>();

        foreach (var partitionGroup in request.Items.GroupBy(item => item.Category))
        {
            var category = partitionGroup.Key;
            var itemIds = partitionGroup
                .Select(item => item.Id)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var query = container.GetItemLinqQueryable<Product>()
                .Where(product => product.Category == category && itemIds.Contains(product.Id))
                .ToFeedIterator();

            var products = new List<Product>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                products.AddRange(response.Where(product => product.DeletedAt is null));
            }

            if (products.Count != itemIds.Count)
            {
                return Results.NotFound(new
                {
                    category,
                    missingIds = itemIds.Except(products.Select(product => product.Id), StringComparer.Ordinal).ToArray()
                });
            }

            var batch = container.CreateTransactionalBatch(new PartitionKey(category));

            foreach (var product in products)
            {
                var updatedProduct = product with
                {
                    Price = request.Price
                };

                batch.ReplaceItem(product.Id, updatedProduct);
            }

            var batchResponse = await batch.ExecuteAsync();

            if (!batchResponse.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)batchResponse.StatusCode);
            }

            updatedCount += products.Count;
            categories.Add(category);
        }

        return Results.Ok(new BatchUpdatePriceResponse(request.Price, updatedCount, categories.AsReadOnly()));
    }

    private static async Task<IResult> UpdateProductAsync(string id, string category, UpdateProductRequest request, Container container)
    {
        Product existingProduct;
        string? eTag;

        try
        {
            var response = await container.ReadItemAsync<Product>(id, new PartitionKey(category));
            existingProduct = response.Resource;
            eTag = response.ETag;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }

        if (existingProduct.DeletedAt is not null)
        {
            return Results.NotFound();
        }

        var updatedProduct = existingProduct with
        {
            Name = request.Name,
            Price = request.Price,
            Quantity = request.Quantity,
            Tags = request.Tags
        };

        try
        {
            var replaceResponse = await container.ReplaceItemAsync(
                updatedProduct,
                id,
                new PartitionKey(category),
                new ItemRequestOptions
                {
                    IfMatchEtag = eTag
                });

            return Results.Ok(replaceResponse.Resource);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            return Results.Conflict(new
            {
                message = "The product was changed by another request. Read the latest version and retry."
            });
        }
    }

    private static async Task<IResult> DeleteProductAsync(string id, string category, Container container)
    {
        Product existingProduct;

        try
        {
            var response = await container.ReadItemAsync<Product>(id, new PartitionKey(category));
            existingProduct = response.Resource;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }

        if (existingProduct.DeletedAt is not null)
        {
            return Results.NotFound();
        }

        var deletedProduct = existingProduct with
        {
            Ttl = 30,
            DeletedAt = DateTimeOffset.UtcNow
        };

        var replaceResponse = await container.ReplaceItemAsync(deletedProduct, id, new PartitionKey(category));

        return Results.Ok(replaceResponse.Resource);
    }

    private static async Task<IResult> DeleteProductPermanentlyAsync(string id, string category, Container container)
    {
        try
        {
            await container.DeleteItemAsync<Product>(id, new PartitionKey(category));
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteProductsByPartitionAsync(string category, Container container)
    {
        var query = container.GetItemLinqQueryable<Product>()
            .Where(product => product.Category == category)
            .Select(product => product.Id)
            .ToFeedIterator();

        var deletedCount = 0;

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();

            foreach (var id in response)
            {
                await container.DeleteItemAsync<Product>(id, new PartitionKey(category));
                deletedCount++;
            }
        }

        return Results.Ok(new
        {
            category,
            deletedCount
        });
    }

    private static async Task<IResult> ExpireProductAsync(string id, string category, Container container)
    {
        try
        {
            var response = await container.PatchItemAsync<Product>(
                id: id,
                partitionKey: new PartitionKey(category),
                patchOperations:
                [
                    PatchOperation.Set("/ttl", 30)
                ]);

            return Results.Ok(response.Resource);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }
    }
}
