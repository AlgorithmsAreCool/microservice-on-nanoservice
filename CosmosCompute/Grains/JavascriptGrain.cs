using Orleans.Runtime;
using Jint;
using System.Net;
using Esprima.Ast;
using Jint.Native.Object;
using System.Text;
using System.Text.Json.Nodes;
using Jint.Runtime.Descriptors;

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
        if (state.State is null)
        {
            return new (HttpStatusCode.NotFound, null);
        }

        if (state.State?.Code is string code)
        {
            Script ??= ParseScriptOrThrow(state.State.Code);

            using var engine = new Engine();
            engine.SetValue("path", path);

            var result = engine.Evaluate(state.State.Code);

            return result.Type switch {
                Jint.Runtime.Types.String => new (HttpStatusCode.OK, result.AsString()),

                _ => new (HttpStatusCode.OK, result.ToString()),
            };
        }
        else
        {
            return new (HttpStatusCode.NotFound, null);
        }
    }

    public class JavascriptGrainState
    {
        public string? Code { get; set; }
    }
}
