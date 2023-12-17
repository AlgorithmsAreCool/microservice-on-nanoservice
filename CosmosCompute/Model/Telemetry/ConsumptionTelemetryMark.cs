namespace CosmosCompute.Model.Telemetry;

public readonly record struct ConsumptionTelemetryMark(long TimestampTicks, ulong CurrentThreadAllocatedBytes)
{
    public ConsumptionTelemetryMeasurement GetConsumptionSince(ConsumptionTelemetryMark other, ulong responseByteCount)
    {
        return new ConsumptionTelemetryMeasurement(
            ResponseByteCount: responseByteCount,
            ExecutionTime: TimeSpan.FromTicks(TimestampTicks - other.TimestampTicks),
            AllocatedBytes: CurrentThreadAllocatedBytes - other.CurrentThreadAllocatedBytes
        );
    }

    public static ConsumptionTelemetryMark Create(TimeProvider timeProvider)
    {
        return new ConsumptionTelemetryMark(timeProvider.GetTimestamp(), (ulong)GC.GetAllocatedBytesForCurrentThread());
    }
}
