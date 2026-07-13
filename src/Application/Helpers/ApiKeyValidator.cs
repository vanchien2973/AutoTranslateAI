using System.Security.Cryptography;
using System.Text;

namespace Application.Helpers;

public static class ApiKeyValidator
{
    public static bool IsAuthorized(string? provided, IReadOnlyCollection<string> configuredKeys)
    {
        if (string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        foreach (var key in configuredKeys)
        {
            if (!string.IsNullOrWhiteSpace(key)
                && CryptographicOperations.FixedTimeEquals(providedBytes, Encoding.UTF8.GetBytes(key)))
            {
                return true;
            }
        }

        return false;
    }
}
