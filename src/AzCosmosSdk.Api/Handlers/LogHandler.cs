using Microsoft.Azure.Cosmos;

namespace AzCosmosSdk.Api.Handlers;

/// <summary>
/// Custom RequestHandler injected into the Cosmos SDK pipeline.
/// Logs every HTTP request the SDK sends and the status code it gets back.
/// </summary>
public sealed class LogHandler(ILogger<LogHandler> logger) : RequestHandler
{
    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Method}]\t{RequestUri}", request.Method.Method, request.RequestUri);

        ResponseMessage response = await base.SendAsync(request, cancellationToken);

        logger.LogInformation("[{StatusCodeNumber}]\t{StatusCode}", Convert.ToInt32(response.StatusCode), response.StatusCode);

        return response;
    }
}
