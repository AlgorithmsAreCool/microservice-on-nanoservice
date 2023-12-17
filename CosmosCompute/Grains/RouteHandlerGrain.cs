using Orleans.Runtime;
using Jint;
using System.Net;
using Esprima.Ast;
using Jint.Native.Object;
using System.Text;
using System.Text.Json.Nodes;
using Jint.Runtime.Descriptors;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;
using CosmosCompute.Model;
using CosmosCompute.Interfaces;
using System.Diagnostics.Contracts;
using Orleans.Serialization.TypeSystem;
using System.Collections.Immutable;
using CosmosCompute.Interfaces.Grains;
using CosmosCompute.Model.Telemetry;

namespace CosmosCompute.Services;

public class RouteHandlerGrain : Grain, IRouteHandlerGrain
{
    public RouteHandlerGrain(
        [PersistentState("state")] IPersistentState<GrainState?> state,
        ScriptCompilerFactory compilerFactory,
        TimeProvider timeProvider)
    {
        PersistentState = state;
        CompilerFactory = compilerFactory;
        TimeProvider = timeProvider;
    }

    private IPersistentState<GrainState?> PersistentState { get; }
    private ScriptCompilerFactory CompilerFactory { get; }
    private TimeProvider TimeProvider { get; }
    private ICompiledScript? CachedCompiledScript { get; set; }


    public async Task UpdateHandlerScript(ScriptLanguage language, string handlerScriptText, string committedBy, string commitMessage)
    {
        var scriptCompiler = CompilerFactory.GetScriptCompiler(ScriptLanguage.Javascript);

        CachedCompiledScript = scriptCompiler.Compile(handlerScriptText);

        PersistentState.State ??= new GrainState();

        var commit = new HistoryCommit(
            new HistoryCommitMetadata(committedBy, commitMessage, DateTimeOffset.UtcNow),
            new PersistedScript(language, scriptCompiler.ApiVersion, handlerScriptText)
        );

        var commitHash = commit.ComputeCommitHash();

        var commitGrain = GrainFactory.GetGrain<IScriptHistoryCommitGrain>(commitHash);
        await commitGrain.SetCommit(commit);

        var historyEntry = new HistoryCommitSummary(
            commit.Metadata,
            new HistoryCommitReference(commitHash)
        );

        PersistentState.State.CommitHistory.Add(historyEntry);

        await PersistentState.WriteStateAsync();//save to storage
    }

    public Task<DataPlaneConsumptionInfo> GetConsumptionInfo()
    {
        if (PersistentState.State is not GrainState stateObj)
            throw new ApplicationException("State is null");

        return Task.FromResult(stateObj.ConsumptionDetail);
    }

    ///<remarks>
    ///This function takes a path string and injects it into the javascript environment.
    ///It then handles the result of the javascript function call and returns a struct for the 
    ///HTTP layer to use.
    /// </remarks>
    public async ValueTask<EvalResult> Execute(string requestPath)
    {
        if (PersistentState.State is not GrainState grainState)
            return new(HttpStatusCode.NotFound, string.Empty);

        if (CachedCompiledScript is ICompiledScript compiledScript)
        {
            return Evaluate(requestPath, grainState, compiledScript);
        }
        else
        {
            return await LoadScriptAndEvaluate(grainState, requestPath);
        }
    }

    private async ValueTask<EvalResult> LoadScriptAndEvaluate(GrainState grainState, string requestPath)
    {
        if (grainState.CommitHistory.Count == 0)
            return new(HttpStatusCode.NotFound, string.Empty);

        var mostRecent = grainState.CommitHistory.Last();

        var commitGrain = GrainFactory.GetGrain<IScriptHistoryCommitGrain>(mostRecent.Reference.Base64CommitHash);

        var maybeCommit = await commitGrain.GetCommit();

        if (maybeCommit is not HistoryCommit commit)
            return new(HttpStatusCode.NotFound, string.Empty);

        var compiler = CompilerFactory.GetScriptCompiler(commit.Script.Language);

        CachedCompiledScript = compiler.Compile(commit.Script.ScriptBody, commit.Script.ScriptApiVersion);

        return Evaluate(requestPath, grainState, CachedCompiledScript);
    }

    private EvalResult Evaluate(string requestPath, GrainState grainState, ICompiledScript compiledScript)
    {
        var context = new ScriptEvaluationContext(requestPath, ImmutableDictionary<string, string>.Empty);

        var startingConsumptionMark = ConsumptionTelemetryMark.Create(TimeProvider);
        EvalResult result = compiledScript.Execute(context);
        var endingConsumptionMark = ConsumptionTelemetryMark.Create(TimeProvider);

        var utf8Size = Encoding.UTF8.GetByteCount(result.Body);

        var consumptionMeasurement = endingConsumptionMark.GetConsumptionSince(startingConsumptionMark, (ulong)utf8Size);

        grainState.ConsumptionDetail = grainState.ConsumptionDetail.WithMeasurement(consumptionMeasurement);

        return result;
    }

    public Task<HistoryCommitSummary> GetScriptSummary()
    {
        if(PersistentState.State is not GrainState stateObj)
            throw new ApplicationException("State is null");

        if (stateObj.CommitHistory.Count == 0)
            throw new ApplicationException("No commits found");

        return Task.FromResult(stateObj.CommitHistory.Last());
    }

    public class GrainState
    {
        public List<HistoryCommitSummary> CommitHistory { get; } = [];
        public DataPlaneConsumptionInfo ConsumptionDetail { get; set; }
    }
}
