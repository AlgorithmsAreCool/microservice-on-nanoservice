using CosmosCompute.Model;

namespace CosmosCompute.Services;

public interface IJavascriptGrain : IGrainWithStringKey
{
    public Task Import(string rawScript, string committedBy, string commitMessage);

    public ValueTask<EvalResult> Execute(string requestPath);

    public Task<DataPlaneConsumptionInfo> GetConsumptionInfo();
}
