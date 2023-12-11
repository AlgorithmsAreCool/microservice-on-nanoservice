using System.Net;

namespace CosmosCompute.Services;

public class DataPlaneRouterService(IClusterClient clusterClient)
{
    /// <summary>
    /// Routes a request to the appropriate handler.
    /// </summary>
    public async Task<EvalResult> DispatchRoute(string prefix, string path)
    {
        if(Helpers.IsValidHandlerId(prefix) is false)
            return new (HttpStatusCode.BadRequest, "Handler ID contains invalid characters");

        var normalizedHandlerName = Helpers.GetNormalizedHandlerName(prefix);

        var grain = clusterClient.GetGrain<IJavascriptGrain>(normalizedHandlerName);

        return await grain.Execute(path);
    }
}
