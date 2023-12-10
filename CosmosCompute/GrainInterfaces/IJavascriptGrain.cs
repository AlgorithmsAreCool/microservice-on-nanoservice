using System.Net;

namespace CosmosCompute.Services;

/// <summary>
/// The result of evaluating a javascript function.
/// </summary>
[GenerateSerializer, Immutable]
public record struct EvalResult(HttpStatusCode StatusCode, string? Body);

public interface IJavascriptGrain : IGrainWithStringKey
{
    public Task Import(string code);

    public Task<EvalResult> Execute(string path);
}
