using Orleans.Runtime;

namespace CosmosCompute.Services;

public class JavascriptGrain(IPersistentState<JavascriptGrain.JavascriptGrainState> state) : Grain, IJavascriptGrain
{
    public async Task Import(string code)
    {
        if (!TryParseJavascript(code, out var error))
            throw new ArgumentException(error);

        state.State.Code = code;
        await state.WriteStateAsync();
    }

    private static bool TryParseJavascript(string code, out string? error)
    {
        
    }

    public class JavascriptGrainState
    {
        public string? Code { get; set; }
    }
}
