namespace AzCosmosSdk.Api.Models;

using Newtonsoft.Json;

public record ProductListResponse(
    IReadOnlyList<Product> Items,
    string? ContinuationToken);

public record CreateBatchProductItem(
    string Name,
    decimal Price,
    int Quantity,
    IReadOnlyList<string> Tags);

public record CreateProductsBatchRequest(
    string Category,
    IReadOnlyList<CreateBatchProductItem> Products);

public record CreateProductsBatchResponse(
    string Category,
    int CreatedCount,
    IReadOnlyList<Product> Products);

public record BatchUpdatePriceItem(
    string Id,
    string Category);

public record BatchUpdatePriceRequest(
    decimal Price,
    IReadOnlyList<BatchUpdatePriceItem> Items);

public record BatchUpdatePriceResponse(
    decimal Price,
    int UpdatedCount,
    IReadOnlyList<string> Categories);

public record CreateProductRequest(
    string Name,
    string Category,
    decimal Price,
    int Quantity,
    IReadOnlyList<string> Tags);

public record UpdateProductRequest(
    string Name,
    decimal Price,
    int Quantity,
    IReadOnlyList<string> Tags);

public record Product(
    string Id,
    string Name,
    string Category,
    decimal Price,
    int Quantity,
    IReadOnlyList<string> Tags,
    int? Ttl = null,
    DateTimeOffset? DeletedAt = null)
{
    [JsonProperty("_etag")]
    public string? ETag { get; init; }
}
