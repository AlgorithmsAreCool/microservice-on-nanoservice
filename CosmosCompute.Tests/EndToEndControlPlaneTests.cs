namespace CosmosCompute.Tests;

using System.Net;
using CosmosCompute.Services;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestingHost;

public class EndToEndControlPlaneTests : IDisposable
{
    public EndToEndControlPlaneTests()
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
    public async Task CanRegisterAndExecuteJavascript()
    {
        var context = GetServerCallContext();

        //Arrange
        var controlPlane = new ControlPlaneService(NullLogger<ControlPlaneService>.Instance, _cluster.Client);
        var request = new RegisterHandlerRequest {
            HandlerId = "test",
            HandlerJsBody = "'hello world';"
        };

        //Act / Assert
        var response = await controlPlane.RegisterHandler(request, context);
        Assert.True(response.Success);

        var dataPlane = new DataPlaneRouterService(_cluster.Client);
        var evalResult = await dataPlane.DispatchRoute("test", "test");

        Assert.Equal(HttpStatusCode.OK, evalResult.StatusCode);
        Assert.Equal("hello world", evalResult.Body);
    }

    [Fact]
    public async Task RegisterHandler_SucceedsOnValidInput()
    {
        var context = GetServerCallContext();

        //Arrange
        var controlPlane = new ControlPlaneService(NullLogger<ControlPlaneService>.Instance, _cluster.Client);
        var request = new RegisterHandlerRequest
        {
            HandlerId = "test",
            HandlerJsBody = "'hello world';"
        };

        //Act
        var response = await controlPlane.RegisterHandler(request, context);

        //Assert

        Assert.True(response.Success);
    }

    [Fact]
    public async Task RegisterHandler_WhenHandlerIdIsInvalid_ReturnsError()
    {
        var context = GetServerCallContext();

        NullLogger<ControlPlaneService> logger = new();
        //Arrange
        var controlPlane = new ControlPlaneService(logger, _cluster.Client);
        var request = new RegisterHandlerRequest
        {
            HandlerId = "invalid id^%$#",
            HandlerJsBody = "console.log('hello world');"
        };

        //Act
        var response = await controlPlane.RegisterHandler(request, context);

        //Assert
        Assert.False(response.Success);
        Assert.Equal("Handler ID contains invalid characters", response.Error);
    }

    private static ServerCallContext GetServerCallContext()
    {
        return TestServerCallContext.Create(
            "RegisterHandler",
            "localhost",
            DateTime.MaxValue,
            new Metadata(),
            CancellationToken.None,
            "localhost",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { }
        );
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
        }
    }

}
