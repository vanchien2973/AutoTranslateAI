using System.Text.Json;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// Facebook Graph API — Upload videos to the Page using a Page access token (multipart: file + metadata).
public sealed class FacebookPublisher : IPublisher
{
    private const string GraphVersion = "v19.0";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storage;

    public FacebookPublisher(IHttpClientFactory httpClientFactory, IStorageService storage)
    {
        _httpClientFactory = httpClientFactory;
        _storage = storage;
    }

    public PublishPlatform Platform => PublishPlatform.Facebook;

    public async Task<PublishOutcome> PublishAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ChannelId))
        {
            throw new InvalidOperationException("A Facebook Page id is required to publish a video.");
        }

        await using var video = await TempMediaFile.DownloadAsync(_storage, request.VideoStorageKey, cancellationToken);
        var http = _httpClientFactory.CreateClient(nameof(FacebookPublisher));

        await using var stream = File.OpenRead(video.Path);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(request.AccessToken), "access_token" },
            { new StringContent(request.Title), "title" },
            { new StringContent(request.Description ?? string.Empty), "description" },
            { new StreamContent(stream), "source", "video.mp4" },
        };

        var endpoint = $"https://graph-video.facebook.com/{GraphVersion}/{request.ChannelId}/videos";
        using var response = await http.PostAsync(endpoint, content, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "Facebook video upload", cancellationToken);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var videoId = json.RootElement.GetProperty("id").GetString()!;
        return new PublishOutcome(videoId, $"https://www.facebook.com/{videoId}");
    }
}
