using Domain.Entities;
using Domain.Enums;

namespace Domain.Tests.Entities;

public class PublishingEntitiesTests
{
    [Fact]
    public void Given_NewPublishResult_When_Created_Then_IsPending()
    {
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.YouTube);
        result.Status.Should().Be(PublishStatus.Pending);
    }

    [Fact]
    public void Given_PublishResult_When_MarkPublished_Then_RecordsExternalIdUrlAndStatus()
    {
        // Arrange
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.YouTube);
        result.MarkFailed("earlier error");

        // Act
        result.MarkPublished("abc123", "https://youtu.be/abc123");

        // Assert
        result.Status.Should().Be(PublishStatus.Published);
        result.ExternalId.Should().Be("abc123");
        result.Url.Should().Be("https://youtu.be/abc123");
        result.ErrorMessage.Should().BeNull();
        result.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Given_PublishResult_When_MarkFailed_Then_RecordsError()
    {
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.Facebook);
        result.MarkFailed("quota exceeded");

        result.Status.Should().Be(PublishStatus.Failed);
        result.ErrorMessage.Should().Be("quota exceeded");
    }

    [Fact]
    public void Given_ChannelConnection_When_UpdateTokens_Then_KeepsRefreshTokenWhenNull()
    {
        // Arrange
        var channel = new ChannelConnection(
            PublishPlatform.YouTube, "chan1", "My Channel", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));

        // Act — refresh a rotated access token without a new refresh token
        channel.UpdateTokens("access-2", refreshToken: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        channel.AccessToken.Should().Be("access-2");
        channel.RefreshToken.Should().Be("refresh-1"); // preserved
    }

    [Fact]
    public void Given_ExpiredExpiry_When_IsExpired_Then_True()
    {
        var now = DateTimeOffset.UtcNow;
        var channel = new ChannelConnection(PublishPlatform.YouTube, "c", "n", "a", "r", now.AddMinutes(-1));

        channel.IsExpired(now).Should().BeTrue();
    }
}
