using CosmosCompute.Model.Telemetry;

namespace CosmosCompute.Model;

[GenerateSerializer, Immutable]
public readonly record struct DataPlaneConsumptionInfo
{
    [Id(0)]
    public ulong TotalRequestCount { get; init; }
    [Id(1)]
    public ulong TotalResponseBytes { get; init; }
    [Id(2)]
    public TimeSpan TotalExecutionTime { get; init; }
    [Id(3)]
    public ulong TotalAllocatedBytes { get; init; }

    
    public ulong TotalConsumptionByteMilliseconds => ulong.CreateTruncating(TotalExecutionTime.TotalMilliseconds) * TotalAllocatedBytes;

    public DataPlaneConsumptionInfo WithMeasurement(ConsumptionTelemetryMeasurement measurement)
    {
        return new DataPlaneConsumptionInfo {
            TotalRequestCount = TotalRequestCount + 1,
            TotalResponseBytes = TotalResponseBytes + measurement.ResponseByteCount,
            TotalExecutionTime = TotalExecutionTime + measurement.ExecutionTime,
            TotalAllocatedBytes = TotalAllocatedBytes + measurement.AllocatedBytes
        };
    }
}