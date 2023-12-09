using Grpc.Core;
using CosmosCompute;

namespace CosmosCompute.Services;

public class ControlPlaneService(ILogger<ControlPlaneService> logger, IClusterClient clusterClient)
    : ControlPlane.ControlPlaneBase
{
    /// <summary>
    /// Registers a new handler with the control plane.
    /// </summary>
    public override async Task<RegisterHandlerResponse> RegisterHandler(RegisterHandlerRequest request, ServerCallContext context)
    {
        //slow, but simple
        var containsInvalidCharacters = request.HandlerId.Any(c => !char.IsLetterOrDigit(c) && c != '-');

        if (containsInvalidCharacters)
        {
            return new RegisterHandlerResponse {
                Success = false,
                Error = "Handler ID contains invalid characters"
            };
        }

        var normalizedHandlerName = request.HandlerId.ToLowerInvariant();

        var grain = clusterClient.GetGrain<IJavascriptGrain>(normalizedHandlerName);

        try
        {
            await grain.Import(request.HandlerJsBody);

            return new RegisterHandlerResponse {
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new RegisterHandlerResponse {
                Success = false,
                Error = ex.Message
            };
        }
        
    }
}
