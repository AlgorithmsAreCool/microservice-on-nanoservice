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

namespace CosmosCompute.Services;

public class JavascriptGrain([PersistentState("state")]  IPersistentState<JavascriptGrain.JavascriptGrainState?> state) : Grain, IJavascriptGrain
{
    private Script? Script { get; set; }
    public async Task Import(string code)
    {
        var script = ParseScriptOrThrow(code);

        Script = script;
        state.State ??= new JavascriptGrainState();
        state.State.Code = code;

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
    public Task<EvalResult> Execute(string path)
    {
        return Task.FromResult(Evaluate(path));
    }

    private EvalResult Evaluate(string path)
    {
        if (state.State is not JavascriptGrainState stateObj)
            return new(HttpStatusCode.NotFound, string.Empty);

        if (stateObj.Code is not string code)
            return new(HttpStatusCode.NotFound, string.Empty);

        Script ??= ParseScriptOrThrow(code);

        using var engine = new Engine();
        InjectApi(path, engine);

        var billingClock = Stopwatch.StartNew();
        var startingMemory = GC.GetAllocatedBytesForCurrentThread();

        var result = engine.Evaluate(Script);

        billingClock.Stop();
        var endingMemory = GC.GetAllocatedBytesForCurrentThread();

        var evalResult = result.Type switch {
            Jint.Runtime.Types.String => new EvalResult(HttpStatusCode.OK, result.AsString()),

            _ => new(HttpStatusCode.OK, result.ToString()),
        };

        var utf8Size = Encoding.UTF8.GetByteCount(evalResult.Body);

        stateObj.ConsumptionDetail = stateObj.ConsumptionDetail.AddRequest(
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
        public DataPlaneConsumptionInfo ConsumptionDetail { get; set; }
        public string? Code { get; set; }
    }
}