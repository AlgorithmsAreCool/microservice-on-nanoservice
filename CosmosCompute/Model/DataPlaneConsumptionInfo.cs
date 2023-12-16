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
    public ulong TotalConsumptionByteMilliseconds { get; init; }

    public DataPlaneConsumptionInfo AddRequest(ulong responseBytes, TimeSpan executionTime, ulong memoryUsageBytes)
    {
        return this with {
            TotalRequestCount = TotalRequestCount + 1,
            TotalResponseBytes = TotalResponseBytes + responseBytes,
            TotalExecutionTime = TotalExecutionTime + executionTime,
            TotalConsumptionByteMilliseconds = TotalConsumptionByteMilliseconds + ulong.CreateTruncating(executionTime.TotalMilliseconds * memoryUsageBytes)
        };
    }
}