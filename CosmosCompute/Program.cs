using CosmosCompute;
using CosmosCompute.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder => {
    siloBuilder.UseLocalhostClustering();
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<ControlPlaneService>();


app.MapGet("/app/{client}/{**rest}", (string client, string rest) =>
{
 return $"Hello World! ({client}) {rest}";
});


app.Run("http://localhost:5000");
