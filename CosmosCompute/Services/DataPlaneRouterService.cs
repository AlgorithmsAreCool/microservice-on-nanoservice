using System.Net;
using CosmosCompute.Model;

namespace CosmosCompute.Services;

public class DataPlaneRouterService(IClusterClient clusterClient)
{
    /// <summary>
    /// Routes a request to the appropriate handler.
    /// </summary>
    public async Task<EvalResult> DispatchRoute(string prefix, string path)
    {
        if(Helpers.IsValidOrganizationName(prefix) is false)
            return new (HttpStatusCode.BadRequest, "Organization Id contains invalid characters");

        var normalizedOrganizationName = Helpers.GetNormalizedOrganizationName(prefix);

        var grain = clusterClient.GetGrain<IJavascriptGrain>(normalizedOrganizationName);

        return await grain.Execute(path);
    }
}
