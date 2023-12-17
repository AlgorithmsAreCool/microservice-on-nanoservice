using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CosmosCompute.Interfaces.Grains;
using CosmosCompute.Model;
using Orleans.Runtime;

namespace CosmosCompute.Services;

public class ScriptHistoryCommitGrain([PersistentState("state")] IPersistentState<ScriptHistoryCommitGrain.GrainState?> state) : Grain, IScriptHistoryCommitGrain
{
    public Task<HistoryCommit?> GetCommit()
    {
        if (state is { RecordExists: true, State.Commit: var commit})
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

        state.State = new GrainState { Commit = commit };
        return state.WriteStateAsync();
    }

    public class GrainState
    {
        public HistoryCommit Commit { get; set; }
    }
}