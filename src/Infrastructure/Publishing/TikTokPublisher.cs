using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// TikTok Content Posting API — direct post: init (receives publish_id + upload_url) then PUT video (one chunk). 
// Note: uploading one chunk is suitable for videos under 64MB; larger files need to be split into multiple chunks (not yet supported).
public sealed class TikTokPublisher : IPublisher
{
    private const string InitEndpoint = "https://open.tiktokapis.com/v2/post/publish/video/init/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storage;

    public TikTokPublisher(IHttpClientFactory httpClientFactory, IStorageService storage)
    {
        _httpClientFactory = httpClientFactory;
        _storage = storage;
    }

    public PublishPlatform Platform => PublishPlatform.TikTok;

    public async Task<PublishOutcome> PublishAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        await using var video = await TempMediaFile.DownloadAsync(_storage, request.VideoStorageKey, cancellationToken);
        var http = _httpClientFactory.CreateClient(nameof(TikTokPublisher));
        http.DefaultRequestHeaders.Authorization = new("Bearer", request.AccessToken);

        var (publishId, uploadUrl) = await InitAsync(http, request, video.Length, cancellationToken);
        await UploadAsync(http, uploadUrl, video.Path, video.Length, cancellationToken);

        return new PublishOutcome(publishId, "https://www.tiktok.com/");
    }

    private static async Task<(string PublishId, string UploadUrl)> InitAsync(
        HttpClient http,
        PublishRequest request,
        long length,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            post_info = new
            {
                title = request.Title,
                privacy_level = "SELF_ONLY",
                disable_comment = false,
            },
            source_info = new
            {
                source = "FILE_UPLOAD",
                video_size = length,
                chunk_size = length,
                total_chunk_count = 1,
            },
        });

        using var response = await http.PostAsync(InitEndpoint, new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "TikTok publish init", cancellationToken);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var data = json.RootElement.GetProperty("data");
        return (data.GetProperty("publish_id").GetString()!, data.GetProperty("upload_url").GetString()!);
    }

    private static async Task UploadAsync(HttpClient http, string uploadUrl, string filePath, long length, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        content.Headers.ContentLength = length;
        content.Headers.ContentRange = new ContentRangeHeaderValue(0, length - 1, length);

        using var upload = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = content };
        using var response = await http.SendAsync(upload, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "TikTok upload", cancellationToken);
    }
}
