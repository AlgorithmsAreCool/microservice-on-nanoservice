// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Security.Cryptography;
using CosmosCompute;

using Grpc.Net.Client;

Console.WriteLine("Hello, World!");

var sampleHtml = @"
`<!DOCTYPE html>
<html>
<head>
    <title>Sample HTML</title>
</head>
<body>
    <h1>Sample HTML</h1>
    <p>This is a sample HTML page from ${path}</p>
</body>
</html>`";


var controlPlanChannel = GrpcChannel.ForAddress("http://localhost:5000", new GrpcChannelOptions{
    Credentials = Grpc.Core.ChannelCredentials.Insecure
});

var controlPlaneClient = new ControlPlane.ControlPlaneClient(controlPlanChannel);


var registerHandler = new RegisterHandlerRequest {
    HandlerId = "echo",
    HandlerJsBody = sampleHtml,
    HandlerRoute = "echo"
};

var registrationResponse = await controlPlaneClient.RegisterHandlerAsync(registerHandler);

Console.WriteLine($"Registration Response: {registrationResponse.Success} {registrationResponse.Error}");


if (registrationResponse.Success)
{
    RunLoad(10000, 1);
    RunLoad(10000, 2);
    RunLoad(10000, 3);
    RunLoad(10000, 4);
    RunLoad(10000, 5);
    RunLoad(10000, 6);
    RunLoad(10000, 7);
    RunLoad(10000, 8);
}

void RunLoad(int requestsPerThread, int threadCount)
{
    var totals = new Totals();
    var totalTimeClock = Stopwatch.StartNew();

    var threads = new Thread[threadCount];
    for (int i = 0; i < threadCount; i++)
    {
        threads[i] = new Thread(() => WorkThread(requestsPerThread, totals).Wait());
        threads[i].Start();
    }
    
    for (int i = 0; i < threadCount; i++)
    {
        threads[i].Join();
    }
    
    totalTimeClock.Stop();

    Console.WriteLine("====================================");
    Console.WriteLine($"Thread Count: {threadCount} Requests Per Thread: {requestsPerThread}");
    Console.WriteLine($"Total: {totals.Total} Errors: {totals.Errors} Subjective Time: {totals.Time.TotalSeconds:N3} Real Time: {totalTimeClock.Elapsed.TotalSeconds:N3}");
    Console.WriteLine($"Subjective Average: {totals.Total / totals.Time.TotalSeconds:N2}");
    Console.WriteLine($"Mean Latency: {totals.Time.TotalMilliseconds / totals.Total:N3}");
    Console.WriteLine($"Real Average: {totals.Total / totalTimeClock.Elapsed.TotalSeconds:N2}");
}

async Task WorkThread(int requestsPerThread, Totals totals)
{
    var dataplaneClient = new HttpClient
    {
        DefaultRequestVersion = new Version(2, 0),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
    };

    try
    {
        //Console.WriteLine($"Thread {Environment.CurrentManagedThreadId} started");
        var sw = Stopwatch.StartNew();
        int counter = 0;
        int errorCounter = 0;
        for (int j = 0; j < requestsPerThread; j++)
        {
            var response = await dataplaneClient.GetAsync("http://localhost:5000/app/echo/echo");
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                // if (j > 0 && j % 1000 == 0)
                //     Console.WriteLine($"Thread {Environment.CurrentManagedThreadId} {j} {j / sw.Elapsed.TotalSeconds}");

                counter++;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {responseString}");
                errorCounter++;
            }
        }

        lock (totals)
        {
            totals.Errors += errorCounter;
            totals.Time += sw.Elapsed;
            totals.Total += counter;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex}");
    }
    finally
    {
        //Console.WriteLine($"Thread {Environment.CurrentManagedThreadId} finished");
    }
    
}






class Totals 
{
    public int Total { get; set; }
    public int Errors { get; set; }
    public TimeSpan Time { get; set; }
}