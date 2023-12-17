using CosmosCompute.Model;

namespace CosmosCompute.Services.Javascript;

public class JavascriptRuntimeFactory(ILogger<JavascriptRuntime> logger, TimeProvider timeProvider)
{
    public JavascriptRuntime Create(ScriptEvaluationContext context)
    {
        return new(logger, timeProvider, context);
    }
}   
