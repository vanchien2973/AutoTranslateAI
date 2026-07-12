namespace Infrastructure.Publishing;

internal static class PublishHttp
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"{operation} failed ({(int)response.StatusCode}): {body}");
    }
}
