using CosmosCompute;
using CosmosCompute.Services;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder => {
    siloBuilder.UseLocalhostClustering();
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<ControlPlaneService>();


app.MapGet("/app/{client}/{**rest}",  async (DataPlaneRouterService routerService, string client, string rest) =>
{
    var evalResult = await routerService.DispatchRoute(client, rest);

    return Results.Text(evalResult.Body, statusCode: (int)evalResult.StatusCode);
});


app.Run("http://localhost:5000");
