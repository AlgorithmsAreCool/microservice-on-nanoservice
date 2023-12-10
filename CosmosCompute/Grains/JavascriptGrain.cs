using Orleans.Runtime;
using Jint;

namespace CosmosCompute.Services;

public class JavascriptGrain(IPersistentState<JavascriptGrain.JavascriptGrainState?> state) : Grain, IJavascriptGrain
{
    public async Task Import(string code)
    {
        EnsureJavascriptParsers(code);

        state.State ??= new JavascriptGrainState();
        state.State.Code = code;

        await state.WriteStateAsync();//save to storage
    }

    private static void EnsureJavascriptParsers(string code)
    {
        var parser = new Esprima.JavaScriptParser();
        _ = parser.ParseScript(code, strict: true);//let it throw
    }

    public class JavascriptGrainState
    {
        public string? Code { get; set; }
    }
}
