using Grpc.Core;
using CosmosCompute;
using CosmosCompute.Model;
using System.Security.Cryptography;
using CosmosCompute.Interfaces.Grains;

namespace CosmosCompute.Services;

public class ControlPlaneService(ILogger<ControlPlaneService> logger, IClusterClient clusterClient)
    : ControlPlane.ControlPlaneBase
{
    public override async Task<GetCurrentConsumptionResponse> GetCurrentConsumption(GetCurrentConsumptionRequest request, ServerCallContext context)
    {
        if (Helpers.IsValidOrganizationName(request.OrganizationId) is false)
        {
            return new GetCurrentConsumptionResponse {
                Error = new Error {
                    Message = "Organization ID contains invalid characters"
                }
            };
        }

        var normalizedHandlerName = Helpers.GetNormalizedOrganizationName(request.OrganizationId);

        var grain = clusterClient.GetGrain<IRouteHandlerGrain>(normalizedHandlerName);

        try
        {
            var consumption = await grain.GetConsumptionInfo();

            return new GetCurrentConsumptionResponse {
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
            logger.LogError(ex, "Failed to query consumption for organization {OrganizationId}", request.OrganizationId);

            return new GetCurrentConsumptionResponse {
                Error = new Error {
                    Message = ex.Message
                }
            };
        }
    }


    /// <summary>
    /// Registers a new handler with the control plane.
    /// </summary>
    public override async Task<CommitRouteHandlerResponse> CommitRouteHandler(CommitRouteHandlerRequest request, ServerCallContext context)
    {
        var language = request.HandlerScriptLanguage switch {
            RouteHandlerLanguage.Javascript => ScriptLanguage.Javascript,

            _ => default
        };

        if (Helpers.IsValidOrganizationName(request.OrganizationId) is false)
        {
            return new CommitRouteHandlerResponse {
                Success = false,
                Error = "Organization ID contains invalid characters"
            };
        }

        var normalizedOrganizationName = Helpers.GetNormalizedOrganizationName(request.OrganizationId);

        var grain = clusterClient.GetGrain<IRouteHandlerGrain>(normalizedOrganizationName);

        try
        {
            await grain.UpdateHandlerScript(language, request.HandlerScriptBody, request.Committer, request.CommitMessage);

            return new CommitRouteHandlerResponse {
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register handler {OrganizationId} {Route}", request.OrganizationId, request.Route);

            return new CommitRouteHandlerResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
        
    }
}
