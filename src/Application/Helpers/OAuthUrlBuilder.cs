using Domain.Enums;

namespace Application.Helpers;

public static class OAuthUrlBuilder
{
    public const string YouTubeScope = "https://www.googleapis.com/auth/youtube.upload";
    public const string FacebookScope = "pages_show_list,pages_manage_posts,pages_read_engagement";
    public const string TikTokScope = "video.publish,video.upload";

    public static string Build(PublishPlatform platform, string clientId, string redirectUri, string state) =>
        platform switch
        {
            PublishPlatform.YouTube => YouTube(clientId, redirectUri, state),
            PublishPlatform.Facebook => Facebook(clientId, redirectUri, state),
            PublishPlatform.TikTok => TikTok(clientId, redirectUri, state),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported publish platform."),
        };

    public static string YouTube(string clientId, string redirectUri, string state) =>
        "https://accounts.google.com/o/oauth2/v2/auth?" + Query(new (string, string)[]
        {
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", YouTubeScope),
            ("access_type", "offline"),
            ("include_granted_scopes", "true"),
            ("prompt", "consent"),
            ("state", state),
        });

    public static string Facebook(string clientId, string redirectUri, string state) =>
        "https://www.facebook.com/v19.0/dialog/oauth?" + Query(new (string, string)[]
        {
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", FacebookScope),
            ("state", state),
        });

    public static string TikTok(string clientId, string redirectUri, string state) =>
        "https://www.tiktok.com/v2/auth/authorize/?" + Query(new (string, string)[]
        {
            ("client_key", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", TikTokScope),
            ("state", state),
        });

    private static string Query(IReadOnlyList<(string Key, string Value)> parameters) =>
        string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
}
