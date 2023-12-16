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
using CosmosCompute.Grains.Interfaces;

namespace CosmosCompute.Services;

public class JavascriptGrain([PersistentState("state")]  IPersistentState<JavascriptGrain.JavascriptGrainState?> state) : Grain, IJavascriptGrain
{
    private static SemanticVersion ScriptApiVersion { get; } = new(0, 0, 1);
    private Script? ParsedScript { get; set; }
    public async Task Import(string code, string committedBy, string commitMessage)
    {
        var script = ParseScriptOrThrow(code);

        ParsedScript = script;
        state.State ??= new JavascriptGrainState();

        var commit = new HistoryCommit(
            new HistoryCommitMetadata(committedBy, commitMessage, DateTimeOffset.UtcNow),
            new PersistedScript(ScriptApiVersion, code)
        );

        var commitHash = commit.ComputeCommitHash();

        var commitGrain = GrainFactory.GetGrain<IScriptHistoryCommitGrain>(commitHash);
        await commitGrain.SetCommit(commit);

        var historyEntry = new HistoryCommitSummary(
            commit.Metadata,
            new HistoryCommitReference(commitHash)
        );

        state.State.CommitHistory.Add(historyEntry);

        await state.WriteStateAsync();//save to storage
    }

    private static Script ParseScriptOrThrow(string code)
    {
        try
        {
            var parser = new Esprima.JavaScriptParser();
            return parser.ParseScript(code, strict: true);
        }
        catch (Esprima.ParserException ex)
        {
            throw new ApplicationException($"Failed to parse javascript: {ex.Message}");
        }
    }

    public Task<DataPlaneConsumptionInfo> GetConsumptionInfo()
    {
        if (state.State is not JavascriptGrainState stateObj)
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
        if (state.State is not JavascriptGrainState grainState)
            return new(HttpStatusCode.NotFound, string.Empty);

        if (ParsedScript is Script parsedScript)
        {
            return Evaluate(requestPath, grainState, parsedScript);
        }
        else
        {
            return await LoadScriptAndEvaluate(grainState, requestPath);
        }
    }

    private async ValueTask<EvalResult> LoadScriptAndEvaluate(JavascriptGrainState grainState, string requestPath)
    {
        if (grainState.CommitHistory.Count == 0)
            return new(HttpStatusCode.NotFound, string.Empty);

        var mostRecent = grainState.CommitHistory.Last();

        var commitGrain = GrainFactory.GetGrain<IScriptHistoryCommitGrain>(mostRecent.Reference.Base64CommitHash);

        var maybeCommit = await commitGrain.GetCommit();

        if (maybeCommit is not HistoryCommit commit)
            return new(HttpStatusCode.NotFound, string.Empty);

        ParsedScript = ParseScriptOrThrow(commit.Script.ScriptBody);

        return Evaluate(requestPath, grainState, ParsedScript);
    }

    private EvalResult Evaluate(string requestPath, JavascriptGrainState grainState, Script parsedScript)
    {
        using var engine = new Engine();
        InjectApi(requestPath, engine);

        var billingClock = Stopwatch.StartNew();
        var startingMemory = GC.GetAllocatedBytesForCurrentThread();

        var result = engine.Evaluate(parsedScript);

        billingClock.Stop();
        var endingMemory = GC.GetAllocatedBytesForCurrentThread();

        var evalResult = result.Type switch {
            Jint.Runtime.Types.String => new EvalResult(HttpStatusCode.OK, result.AsString()),

            _ => new(HttpStatusCode.OK, result.ToString()),
        };

        var utf8Size = Encoding.UTF8.GetByteCount(evalResult.Body);

        grainState.ConsumptionDetail = grainState.ConsumptionDetail.AddRequest(
             (ulong)utf8Size,
             billingClock.Elapsed,
             (ulong)(endingMemory - startingMemory)
         );

        return evalResult;
    }

    private static void InjectApi(string path, Engine engine)
    {
        engine.SetValue("path", path);
        engine.SetValue("fetch", (string resource) =>
        {
            using var client = new HttpClient();
            return client.GetStringAsync(resource).Result;
        });
    }

    public class JavascriptGrainState
    {
        public List<HistoryCommitSummary> CommitHistory { get; } = [];
        public DataPlaneConsumptionInfo ConsumptionDetail { get; set; }
    }
}
