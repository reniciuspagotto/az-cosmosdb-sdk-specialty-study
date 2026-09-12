using System.Net;

namespace AzCosmosSdk.BulkInsert.Models;

internal sealed record BulkInsertItemResult(string ProductId, HttpStatusCode StatusCode, string? ErrorMessage)
{
	public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

internal sealed record BulkInsertResult(
	int SuccessCount,
	int FailureCount,
	IReadOnlyList<BulkInsertItemResult> Failures);