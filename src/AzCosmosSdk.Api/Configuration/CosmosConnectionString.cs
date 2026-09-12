namespace AzCosmosSdk.Api.Configuration;

public static class CosmosConnectionString
{
    // Extracts the endpoint for safe logging; never expose the AccountKey segment.
    public static string ParseAccountEndpoint(string connectionString)
    {
        const string endpointPrefix = "AccountEndpoint=";

        var endpointSegment = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(segment => segment.StartsWith(endpointPrefix, StringComparison.OrdinalIgnoreCase));

        return endpointSegment is null
            ? "<missing>"
            : endpointSegment[endpointPrefix.Length..];
    }
}
