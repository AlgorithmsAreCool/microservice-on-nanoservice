using Grpc.Core;
using CosmosCompute;

namespace CosmosCompute.Services;

public class ControlPlaneService(ILogger<ControlPlaneService> logger)
    : ControlPlane.ControlPlaneBase
{

}
