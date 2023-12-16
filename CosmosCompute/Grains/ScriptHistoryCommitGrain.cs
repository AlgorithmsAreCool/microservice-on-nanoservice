using System.Runtime.Versioning;

using CosmosCompute.Grains.Interfaces;
using CosmosCompute.Model;
using Orleans.Runtime;

namespace CosmosCompute.Services;

public class ScriptHistoryCommitGrain([PersistentState("state")] IPersistentState<HistoryCommit?> state) : Grain, IScriptHistoryCommitGrain
{
    public Task<HistoryCommit?> GetCommit()
    {
        if (state.State is HistoryCommit commit)
        {
            return Task.FromResult<HistoryCommit?>(commit);
        }
        else
        {
            return Task.FromResult<HistoryCommit?>(null);
        }
    }

    public Task SetCommit(HistoryCommit commit)
    {
        var localKey = this.GetPrimaryKeyString();
        var hash = commit.ComputeCommitHash();
        if (localKey != hash)
            throw new InvalidOperationException();

        state.State = commit;
        return state.WriteStateAsync();
    }
}