using CosmosCompute.Model;

namespace CosmosCompute.Interfaces.Grains;

public interface IRouteHandlerGrain : IGrainWithStringKey
{
    public Task<HistoryCommitSummary> GetScriptSummary();
    public Task UpdateHandlerScript(ScriptLanguage language, string handlerScriptText, string committedBy, string commitMessage);

    public ValueTask<EvalResult> Execute(string requestPath);

    public Task<DataPlaneConsumptionInfo> GetConsumptionInfo();
}
