namespace Application.Features.Publishing.GetChannelAuthUrl;

public sealed record GetChannelAuthUrlResponse(AuthUrlStatus Status, string? Url, string? State, string? Error)
{
    public static GetChannelAuthUrlResponse Ok(string url, string state) => new(AuthUrlStatus.Ok, url, state, null);

    public static GetChannelAuthUrlResponse CredentialsMissing(string platform) =>
        new(AuthUrlStatus.CredentialsMissing, null, null, $"No app credentials configured for {platform}. Set them first via PUT /api/publishing/credentials/{platform}.");
}
