namespace Infrastructure.Storage;

internal static class R2UrlResolver
{
    public static string Resolve(string? publicUrl, string key)
    {
        var normalizedKey = key.TrimStart('/');
        return string.IsNullOrWhiteSpace(publicUrl)
            ? normalizedKey
            : $"{publicUrl.TrimEnd('/')}/{normalizedKey}";
    }
}
