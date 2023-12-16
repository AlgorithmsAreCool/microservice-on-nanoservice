using System.Net;

namespace CosmosCompute.Model;

/// <summary>
/// The result of evaluating a javascript function.
/// </summary>
[GenerateSerializer, Immutable]
public record struct EvalResult(HttpStatusCode StatusCode, string Body);
