using CosmosCompute.Model;
using CosmosCompute.Services;

namespace CosmosCompute.Interfaces;

public interface ICompiledScript
{
    public EvalResult Execute(ScriptEvaluationContext context);
}
