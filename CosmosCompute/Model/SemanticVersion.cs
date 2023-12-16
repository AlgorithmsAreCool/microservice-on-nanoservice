namespace CosmosCompute.Model;

[GenerateSerializer, Immutable]
public record SemanticVersion(int Major, int Minor, int Patch) : IComparable<SemanticVersion>
{
    public bool IsCompatibleWith(SemanticVersion other)
    {
        if (Major != other.Major)
            return false;

        if (Minor < other.Minor)
            return false;

        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
            return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
            return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
            return minorComparison;

        return Patch.CompareTo(other.Patch);
    }
    
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
