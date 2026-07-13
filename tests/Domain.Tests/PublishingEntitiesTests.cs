using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class PublishingEntitiesTests
{
    [Fact]
    public void ChannelConnection_UpdateTokens_RefreshesAndTracksTime()
    {
        var now = DateTimeOffset.UtcNow;
        var connection = new ChannelConnection(PublishPlatform.YouTube, "chid", "name", "old", "old-refresh", now.AddMinutes(-1));

        connection.IsExpired(now).Should().BeTrue();

        connection.UpdateTokens("new-access", null, now.AddHours(1));

        connection.AccessToken.Should().Be("new-access");
        connection.RefreshToken.Should().Be("old-refresh"); // null refresh keeps the old one
        connection.IsExpired(now).Should().BeFalse();
        connection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void PublishResult_MarkFailed_SetsError()
    {
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.Facebook);
        result.MarkPublishing();
        result.MarkFailed("quota exceeded");

        result.Status.Should().Be(PublishStatus.Failed);
        result.ErrorMessage.Should().Be("quota exceeded");
    }

    [Fact]
    public void PlatformCredential_Update_ReplacesKeysAndTouches()
    {
        var credential = new PlatformCredential(PublishPlatform.TikTok, "old", "old-secret", null);

        credential.Update("new", "new-secret", "https://cb");

        credential.ClientId.Should().Be("new");
        credential.ClientSecret.Should().Be("new-secret");
        credential.DefaultRedirectUri.Should().Be("https://cb");
        credential.UpdatedAt.Should().NotBeNull();
    }
}
