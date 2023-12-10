namespace CosmosCompute.Tests;

using CosmosCompute.Services;

using Esprima;

using Orleans;
using Orleans.Runtime;

public class JavascriptGrainTests
{
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