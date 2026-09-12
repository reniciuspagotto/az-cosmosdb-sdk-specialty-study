namespace AzCosmosSdk.ChangeFeed.Models;

internal sealed record Product(
	string Id,
	string Name,
	string Category,
	decimal Price,
	int Quantity,
	IReadOnlyList<string> Tags,
	int? Ttl = null,
	DateTimeOffset? DeletedAt = null);