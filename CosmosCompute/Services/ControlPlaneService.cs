using Grpc.Core;
using CosmosCompute;

namespace CosmosCompute.Services;

public class ControlPlaneService(ILogger<ControlPlaneService> logger, IClusterClient clusterClient)
    : ControlPlane.ControlPlaneBase
{
    public override async Task<QueryConsumptionResponse> QueryUsage(QueryConsumptionRequest request, ServerCallContext context)
    {
        if (Helpers.IsValidHandlerId(request.HandlerId) is false)
        {
            return new QueryConsumptionResponse
            {
                Error = new Error
                {
                    Message = "Handler ID contains invalid characters"
                }
            };
        }

        var normalizedHandlerName = Helpers.GetNormalizedHandlerName(request.HandlerId);

        var grain = clusterClient.GetGrain<IJavascriptGrain>(normalizedHandlerName);

        try
        {
            var consumption = await grain.GetConsumptionInfo();

            return new QueryConsumptionResponse
            {
                Details = new ConsumptionDetail {
                    TotalRequestCount = consumption.TotalRequestCount,
                    TotalExecutionTimeMicroseconds = ulong.CreateTruncating(consumption.TotalExecutionTime.TotalMicroseconds),
                    TotalResponseBytes = consumption.TotalResponseBytes,
                    TotalConsumptionByteMilliseconds = consumption.TotalConsumptionByteMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query consumption for handler {HandlerId}", request.HandlerId);

            return new QueryConsumptionResponse
            {
                Error = new Error
                {
                    Message = ex.Message
                }
            };
        }
    }


    /// <summary>
    /// Registers a new handler with the control plane.
    /// </summary>
    public override async Task<RegisterHandlerResponse> RegisterHandler(RegisterHandlerRequest request, ServerCallContext context)
    {
        if (Helpers.IsValidHandlerId(request.HandlerId) is false)
        {
            return new RegisterHandlerResponse {
                Success = false,
                Error = "Handler ID contains invalid characters"
            };
        }

        var normalizedHandlerName = Helpers.GetNormalizedHandlerName(request.HandlerId);

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
            logger.LogError(ex, "Failed to register handler {HandlerId}", request.HandlerId);

            return new RegisterHandlerResponse {
                Success = false,
                Error = ex.Message
            };
        }
        
    }
}
