using System.Text;
using CosmosCompute.Model;

namespace CosmosCompute.Grains.Interfaces;

public interface IScriptHistoryCommitGrain : IGrainWithStringKey
{
    public Task<HistoryCommit?> GetCommit();
    public Task SetCommit(HistoryCommit commit);
}
