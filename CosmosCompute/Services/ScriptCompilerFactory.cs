using CosmosCompute.Model;
using CosmosCompute.Interfaces;
using CosmosCompute.Services.Javascript;

namespace CosmosCompute.Services;

public class ScriptCompilerFactory(JavascriptRuntimeFactory javascriptRuntimeFactory)
{
    public IScriptCompiler GetScriptCompiler(ScriptLanguage scriptLanguage)
    {
        return scriptLanguage switch {
            ScriptLanguage.Javascript => new JavascriptScriptCompiler(javascriptRuntimeFactory),
            _ => throw new NotSupportedException($"Script language {scriptLanguage} is not supported")
        };
    }
}
