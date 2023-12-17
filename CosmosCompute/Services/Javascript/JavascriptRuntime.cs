using System.Net;
using CosmosCompute.Model;
using Esprima.Ast;
using Jint;

namespace CosmosCompute.Services.Javascript;

public class JavascriptRuntime : IDisposable
{
    public JavascriptRuntime(ILogger<JavascriptRuntime> logger, TimeProvider timeProvider, ScriptEvaluationContext context)
    {
        this.Logger = logger;
        this.TimeProvider = timeProvider;
        this.Engine = new Engine();

        AttachRuntimeHooks(Engine);
        AttachRequestContext(Engine, context);
    }

    private ILogger<JavascriptRuntime> Logger { get; }
    private TimeProvider TimeProvider { get; }

    private Engine Engine { get; } = new();
    //public TimeSpan NonBlockingTime { get; }


    public EvalResult Evaluate(Script script)
    {
        var result = Engine.Evaluate(script);
        
        return result.Type switch {
            Jint.Runtime.Types.String => new EvalResult(HttpStatusCode.OK, result.AsString()),

            _ => new(HttpStatusCode.OK, result.ToString()),
        };
    }

    private void AttachRuntimeHooks(Engine engine)
    {
        engine.SetValue("fetch", new Func<string, string>(Fetch));
    }

    private void AttachRequestContext(Engine engine, ScriptEvaluationContext context)
    {
        engine.SetValue("path", context.RequestPath);
    }

    private string Fetch(string resource)
    {
        //TODO: This is a blocking call. We need to make this non-blocking.
        //TODO : Record the non-blocking time of this call

        using var client = new HttpClient();
        var startTime = TimeProvider.GetTimestamp();    

        var result = client.GetStringAsync(resource).Result;

        var endTime = TimeProvider.GetTimestamp();
        //NonBlockingTime = TimeSpan.FromTicks(endTime - startTime);
        
        return result;
    }

    public void Dispose() => Engine.Dispose();
}
