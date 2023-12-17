namespace CosmosCompute.Tests;

using CommunityToolkit.Diagnostics;
using CosmosCompute.Interfaces.Grains;
using CosmosCompute.Services;
using CosmosCompute.Services.Javascript;
using Esprima;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.TestingHost;

public class JavascriptGrainTests
{
 public static string LargeSampleJavascriptSnippet = 
 @"function fib(n) {
        if (n < 2) {
            return n;
        }
        return fib(n - 1) + fib(n - 2);
    }
    function quicksort(arr) {
        if (arr.length <= 1) {
            return arr;
        }
        var pivot = arr[0];
        var left = [];
        var right = [];
        for (var i = 1; i < arr.length; i++) {
            arr[i] < pivot ? left.push(arr[i]) : right.push(arr[i]);
        }
        return quicksort(left).concat(pivot, quicksort(right));
    }

    function main() {
        var arr = [];
        for (var i = 0; i < 10000; i++) {
            arr.push(Math.floor(Math.random() * 10000));
        }
        quicksort(arr);
        return arr;
    }
    main();
    ";

    public JavascriptGrainTests()
    {
        var testClusterBuilder = new TestClusterBuilder
        {
            Options = {
                InitialSilosCount = 1,
            }
        };

        testClusterBuilder.AddSiloBuilderConfigurator<TestSiloConfigurator>();

        _cluster = testClusterBuilder.Build();
        _cluster.Deploy();
    }

    private readonly TestCluster _cluster;


    [Fact]
    public async Task ConsumptionInfoIsUpdated()
    {
        //Arrange
        var grain = _cluster.GrainFactory.GetGrain<IRouteHandlerGrain>(string.Empty);

        //Act
        await grain.UpdateHandlerScript(language: Model.ScriptLanguage.Javascript, LargeSampleJavascriptSnippet, "", "");

        var initialConsumptionInfo = await grain.GetConsumptionInfo();
        for (var i = 0; i < 10; i++)
        {
            await grain.Execute("test");
        }

        var finalConsumptionInfo = await grain.GetConsumptionInfo();


        //Assert
        Guard.IsGreaterThan(finalConsumptionInfo.TotalRequestCount, initialConsumptionInfo.TotalRequestCount);
        Guard.IsGreaterThan(finalConsumptionInfo.TotalExecutionTime, initialConsumptionInfo.TotalExecutionTime);
        Guard.IsGreaterThan(finalConsumptionInfo.TotalResponseBytes, initialConsumptionInfo.TotalResponseBytes);
        Guard.IsGreaterThan(finalConsumptionInfo.TotalConsumptionByteMilliseconds, initialConsumptionInfo.TotalConsumptionByteMilliseconds);
    }

    [Fact]
    public async Task CanExecuteFetch()
    {
        //Arrange
        var code = "fetch('https://example.org/')";
        var grain = _cluster.GrainFactory.GetGrain<IRouteHandlerGrain>(string.Empty);

        //Act
        await grain.UpdateHandlerScript(Model.ScriptLanguage.Javascript, code, "", "");
        var result = await grain.Execute("test");

        //Assert
        Guard.IsNotNullOrWhiteSpace(result.Body);
    }

    [Fact]
    public async Task CanExecuteExpression()
    {
        //Arrange
        var code = "2 * 2";
        var grain = _cluster.GrainFactory.GetGrain<IRouteHandlerGrain>(string.Empty);

        //Act
        await grain.UpdateHandlerScript(Model.ScriptLanguage.Javascript, code, "", "");
        var result = await grain.Execute("test");

        //Assert
        Guard.IsEqualTo(result.Body, "4");
    }

    [Fact]
    public async Task Import_WhenCodeIsValid_ImportsCode()
    {
        //Arrange
        var code = "console.log('hello world');";
        var grain = _cluster.GrainFactory.GetGrain<IRouteHandlerGrain>(string.Empty);

        //Act
        await grain.UpdateHandlerScript(Model.ScriptLanguage.Javascript, code, "testcommiter", "");

        //Assert
        var lastCommit = await grain.GetScriptSummary();

        Assert.Equal("testcommiter", lastCommit.Metadata.CommittedBy);
    }

    [Fact]
    public async Task Import_WhenCodeIsInvalid_Throws()
    {
        //Arrange
        var code = "console.log('hello";
        var grain = _cluster.GrainFactory.GetGrain<IRouteHandlerGrain>(string.Empty);

        //Act / Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => grain.UpdateHandlerScript(Model.ScriptLanguage.Javascript, code, "", ""));
    }

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.Services.AddSingleton(TimeProvider.System);
            siloBuilder.Services.AddSingleton<JavascriptRuntimeFactory>();
            siloBuilder.Services.AddSingleton<ScriptCompilerFactory>();
        }
    }
}