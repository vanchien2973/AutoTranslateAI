using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// YouTube Data API v3 — resumable upload: init session (metadata) then PUT all bytes video.
public sealed class YouTubePublisher : IPublisher
{
    private const string ResumableEndpoint =
        "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storage;

    public YouTubePublisher(IHttpClientFactory httpClientFactory, IStorageService storage)
    {
        _httpClientFactory = httpClientFactory;
        _storage = storage;
    }

    public PublishPlatform Platform => PublishPlatform.YouTube;

    public async Task<PublishOutcome> PublishAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        await using var video = await TempMediaFile.DownloadAsync(_storage, request.VideoStorageKey, cancellationToken);
        var http = _httpClientFactory.CreateClient(nameof(YouTubePublisher));
        http.DefaultRequestHeaders.Authorization = new("Bearer", request.AccessToken);

        var uploadUrl = await StartSessionAsync(http, request, video.Length, cancellationToken);
        return await UploadAsync(http, uploadUrl, video.Path, cancellationToken);
    }

    private static async Task<Uri> StartSessionAsync(HttpClient http, PublishRequest request, long length, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            snippet = new
            {
                title = request.Title,
                description = request.Description ?? string.Empty,
                tags = request.Tags,
            },
            status = new { privacyStatus = "private" },
        });

        using var start = new HttpRequestMessage(HttpMethod.Post, ResumableEndpoint)
        {
            Content = new StringContent(metadata, Encoding.UTF8, "application/json"),
        };
        start.Headers.TryAddWithoutValidation("X-Upload-Content-Type", "video/*");
        start.Headers.TryAddWithoutValidation("X-Upload-Content-Length", length.ToString());

        using var response = await http.SendAsync(start, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "YouTube resumable session", cancellationToken);

        return response.Headers.Location
            ?? throw new InvalidOperationException("YouTube did not return a resumable upload URL.");
    }

    private static async Task<PublishOutcome> UploadAsync(HttpClient http, Uri uploadUrl, string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("video/*");

        using var upload = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = content };
        using var response = await http.SendAsync(upload, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "YouTube upload", cancellationToken);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var videoId = json.RootElement.GetProperty("id").GetString()!;
        return new PublishOutcome(videoId, $"https://youtu.be/{videoId}");
    }
}
