using CosmosCompute.Model;

namespace CosmosCompute.Interfaces;
public interface IScriptCompiler
{
    public string Name { get; }
    public SemanticVersion ApiVersion { get; }
    public ICompiledScript Compile(string scriptText, SemanticVersion? scriptApiVersion = default);
}
