using System.Net;

namespace CosmosCompute.Model;

/// <summary>
/// The result of evaluating a javascript function.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct EvalResult(HttpStatusCode StatusCode, string Body);
