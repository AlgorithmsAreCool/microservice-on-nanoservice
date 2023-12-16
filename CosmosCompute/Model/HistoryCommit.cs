using System.Formats.Cbor;

namespace CosmosCompute.Model;

[GenerateSerializer, Immutable]
public readonly record struct HistoryCommitReference(string Base64CommitHash);

[GenerateSerializer, Immutable]
public readonly record struct PersistedScript(SemanticVersion ScriptApiVersion, string ScriptBody);

[GenerateSerializer, Immutable]
public readonly record struct HistoryCommitMetadata(string CommittedBy, string Message, DateTimeOffset CommitDate);

[GenerateSerializer, Immutable]
public readonly record struct HistoryCommitSummary(HistoryCommitMetadata Metadata, HistoryCommitReference Reference);

[GenerateSerializer, Immutable]
public readonly record struct HistoryCommit(HistoryCommitMetadata Metadata, PersistedScript Script)
{
    public HistoryCommitReference GetReference() => new(ComputeCommitHash());

    ///<remarks
    ///We use CBOR in canonical mode to ensure that the hash is the same 
    public string ComputeCommitHash() {
        var writer = new CborWriter(
            conformanceMode: CborConformanceMode.Canonical, 
            convertIndefiniteLengthEncodings: true
        );

        writer.WriteStartMap(null);
        
        writer.WriteTextString("ScriptApiVersion");
        writer.WriteTextString(Script.ScriptApiVersion.ToString());

        writer.WriteTextString("ScriptBody");
        writer.WriteTextString(Script.ScriptBody);

        writer.WriteTextString("CommittedBy");
        writer.WriteTextString(Metadata.CommittedBy);

        writer.WriteTextString("Message");
        writer.WriteTextString(Metadata.Message);

        writer.WriteTextString("CommitDate");
        writer.WriteTextString(Metadata.CommitDate.ToString("O"));

        writer.WriteEndMap();

        var hash = System.Security.Cryptography.SHA256.HashData(writer.Encode());

        return Convert.ToBase64String(hash);
    }
}
