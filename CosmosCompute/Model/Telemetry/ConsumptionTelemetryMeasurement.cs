namespace CosmosCompute.Model.Telemetry;

public readonly record struct ConsumptionTelemetryMeasurement(ulong ResponseByteCount, TimeSpan ExecutionTime, ulong AllocatedBytes);
