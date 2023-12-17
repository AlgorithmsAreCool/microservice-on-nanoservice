namespace CosmosCompute.Tests;

using System.Net;
using CosmosCompute.Services;
using CosmosCompute.Services.Javascript;
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
        var request = new CommitRouteHandlerRequest {
            OrganizationId = "test",
            HandlerScriptBody = "'hello world';",
            HandlerScriptLanguage = RouteHandlerLanguage.Javascript,
            Committer = "test client",
            Route = "/test",
            CommitMessage = "test commit"
        };

        //Act / Assert
        var response = await controlPlane.CommitRouteHandler(request, context);
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
        var request = new CommitRouteHandlerRequest {
            OrganizationId = "test",
            HandlerScriptBody = "'hello world';",
            HandlerScriptLanguage = RouteHandlerLanguage.Javascript,
            Committer = "test client",
            Route = "/test",
            CommitMessage = "test commit"
        };

        //Act
        var response = await controlPlane.CommitRouteHandler(request, context);

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
        var request = new CommitRouteHandlerRequest
        {
            OrganizationId = "invalid id^%$#",
            HandlerScriptBody = "'hello world';",
            HandlerScriptLanguage = RouteHandlerLanguage.Javascript,
            Committer = "test client",
            Route = "/test",
            CommitMessage = "test commit"
        };

        //Act
        var response = await controlPlane.CommitRouteHandler(request, context);

        //Assert
        Assert.False(response.Success);
        Assert.Equal("Organization ID contains invalid characters", response.Error);
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
            siloBuilder.Services.AddSingleton(TimeProvider.System);
            siloBuilder.Services.AddSingleton<JavascriptRuntimeFactory>();
            siloBuilder.Services.AddSingleton<ScriptCompilerFactory>();
        }
    }
}
