namespace CosmosCompute.Services;

public interface IJavascriptGrain : IGrainWithStringKey
{
    public Task Import(string code);
}
