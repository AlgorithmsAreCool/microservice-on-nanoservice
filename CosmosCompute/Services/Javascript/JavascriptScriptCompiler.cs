using Jint;
using System.Net;
using Esprima.Ast;
using System.Diagnostics;
using CosmosCompute.Model;
using CosmosCompute.Interfaces;
using CommunityToolkit.Diagnostics;

namespace CosmosCompute.Services.Javascript;

public class JavascriptScriptCompiler : IScriptCompiler
{
    public JavascriptScriptCompiler(JavascriptRuntimeFactory runtimeFactory)
    {
        RuntimeFactory = runtimeFactory;
    }

    private JavascriptRuntimeFactory RuntimeFactory { get; }

    public string Name => "Jint Javascript Engine";
    public SemanticVersion ApiVersion => new(0, 0, 1);
    

    public ICompiledScript Compile(string scriptText, SemanticVersion? semanticVersion = default)
    {
        var version = semanticVersion ?? ApiVersion;

        Guard.IsTrue(version.IsCompatibleWith(ApiVersion));

        var script = ParseScriptOrThrow(scriptText);

        return new JavascriptCompiledScript(RuntimeFactory, script);
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

    private class JavascriptCompiledScript(JavascriptRuntimeFactory runtimeFactory, Script script) : ICompiledScript
    {
        public EvalResult Execute(ScriptEvaluationContext context)
        {
            using var runtime = runtimeFactory.Create(context);

            return runtime.Evaluate(script);
        }
    }
}
