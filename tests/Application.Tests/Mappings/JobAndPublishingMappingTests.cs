using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Mappings;

public class JobAndPublishingMappingTests
{
    [Fact]
    public void JobMapping_ToSummary_MapsCoreFields()
    {
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true);

        var dto = JobMapping.ToSummary(job);

        dto.Id.Should().Be(job.Id);
        dto.Status.Should().Be(job.Status.ToString());
        dto.SourceUrl.Should().Be("https://youtu.be/x");
    }

    [Fact]
    public void PublishingMapping_Channel_ToDto_ReportsExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var connection = new ChannelConnection(
            PublishPlatform.YouTube, "chid", "My Channel", "token", "refresh", now.AddHours(-1));

        var dto = connection.ToDto(now);

        dto.Platform.Should().Be(PublishPlatform.YouTube);
        dto.ChannelName.Should().Be("My Channel");
        dto.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void PublishingMapping_Result_ToDto_MapsStatusAndUrl()
    {
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.TikTok);
        result.MarkPublished("ext-1", "https://tiktok/x");

        var dto = result.ToDto();

        dto.Platform.Should().Be(PublishPlatform.TikTok);
        dto.Status.Should().Be(PublishStatus.Published);
        dto.ExternalId.Should().Be("ext-1");
        dto.Url.Should().Be("https://tiktok/x");
    }
}
