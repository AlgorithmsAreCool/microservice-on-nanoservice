namespace CosmosCompute.Services;

public static class Helpers
{
    public static string GetNormalizedOrganizationName(string handlerId)
    {
        return handlerId.ToLowerInvariant();
    }
    
    public static bool IsValidOrganizationName(string oranizationId)
    {
        //slow, but simple
        var containsInvalidCharacters = oranizationId.Any(c => !char.IsLetterOrDigit(c) && c != '-');

        return !containsInvalidCharacters;
    }
}
