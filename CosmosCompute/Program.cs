using CosmosCompute;
using CosmosCompute.Services;
using CosmosCompute.Services.Javascript;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder => {
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorageAsDefault();
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<DataPlaneRouterService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<JavascriptRuntimeFactory>();
builder.Services.AddSingleton<ScriptCompilerFactory>();

var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<ControlPlaneService>();


app.MapGet("/app/{client}/{**rest}", async ([FromServices] DataPlaneRouterService routerService, string client, string rest) => {
    var evalResult = await routerService.DispatchRoute(client, rest);

    return Results.Text(evalResult.Body, statusCode: (int)evalResult.StatusCode);
});


app.Run("http://localhost:5000");
