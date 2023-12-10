namespace CosmosCompute.Services;

public static class Helpers
{
    public static string GetNormalizedHandlerName(string handlerId)
    {
        return handlerId.ToLowerInvariant();
    }
    
    public static bool IsValidHandlerId(string handlerId)
    {
        //slow, but simple
        var containsInvalidCharacters = handlerId.Any(c => !char.IsLetterOrDigit(c) && c != '-');

        return !containsInvalidCharacters;
    }
}
