namespace CosmosCompute.Tests;

using CommunityToolkit.Diagnostics;
using CosmosCompute.Services;

using Esprima;

using Orleans;
using Orleans.Runtime;

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

    [Fact]
    public async Task ConsumptionInfoIsUpdated()
    {
        //Arrange
        
        var state = new MockStorage<JavascriptGrain.JavascriptGrainState?>();
        var grain = new JavascriptGrain(state);

        //Act
        await grain.Import(LargeSampleJavascriptSnippet);
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
        var state = new MockStorage<JavascriptGrain.JavascriptGrainState?>();
        var grain = new JavascriptGrain(state);

        //Act
        await grain.Import(code);
        var result = await grain.Execute("test");

        //Assert
        Assert.Equal(code, state.State?.Code);
        Assert.Null(result.Body);
    }

    [Fact]
    public async Task CanExecuteExpression()
    {
        //Arrange
        var code = "2 * 2";
        var state = new MockStorage<JavascriptGrain.JavascriptGrainState?>();
        var grain = new JavascriptGrain(state);

        //Act
        await grain.Import(code);
        var result = await grain.Execute("test");

        //Assert
        Assert.Equal(code, state.State?.Code);
        Assert.Null(result.Body);
    }

    [Fact]
    public async Task Import_WhenCodeIsValid_ImportsCode()
    {
        //Arrange
        var code = "console.log('hello world');";
        var state = new MockStorage<JavascriptGrain.JavascriptGrainState?>();
        var grain = new JavascriptGrain(state);

        //Act
        await grain.Import(code);

        //Assert
        Assert.Equal(code, state.State?.Code);
    }

    [Fact]
    public async Task Import_WhenCodeIsInvalid_Throws()
    {
        //Arrange
        var code = "console.log('hello";
        var state = new MockStorage<JavascriptGrain.JavascriptGrainState?>();
        var grain = new JavascriptGrain(state);

        //Act / Assert
        var ex = await Assert.ThrowsAsync<ParserException>(() => grain.Import(code));
    }
}

public class MockStorage<T> : IPersistentState<T?>
{
    public T? State { get; set; }
    public string? Etag { get; }
    public bool RecordExists { get; private set; }

    public Task ClearStateAsync()
    {
        RecordExists = false;
        State = default;
        return Task.CompletedTask;
    }

    public Task ReadStateAsync()
    {
        return Task.CompletedTask;
    }

    public Task WriteStateAsync()
    {
        RecordExists = true;
        return Task.CompletedTask;
    }
}