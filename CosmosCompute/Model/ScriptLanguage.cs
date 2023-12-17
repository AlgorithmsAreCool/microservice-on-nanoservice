namespace CosmosCompute.Model;

public enum ScriptLanguage
{
    Default = default,
    
    Javascript,
    Liquid
}


public readonly record struct ScriptEvaluationContext(string RequestPath, IReadOnlyDictionary<string, string> ScriptEnvironment);