using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class ChannelConnectionTokenTests
{
    private static ChannelConnection Connected(DateTimeOffset? expiresAt, string? refreshToken = "stored-refresh") =>
        new(PublishPlatform.YouTube, "UC123", "My channel", "old-access", refreshToken, expiresAt);

    [Fact]
    public void Given_RefreshResponseWithoutNewToken_When_UpdateTokens_Then_KeepsStoredRefresh()
    {
        var connection = Connected(DateTimeOffset.UtcNow.AddMinutes(-5));

        connection.UpdateTokens("new-access", null, DateTimeOffset.UtcNow.AddHours(1));

        connection.AccessToken.Should().Be("new-access");
        connection.RefreshToken.Should().Be("stored-refresh");
    }

    [Fact]
    public void Given_RotatedRefreshToken_When_UpdateTokens_Then_StoresNewOne()
    {
        var connection = Connected(DateTimeOffset.UtcNow.AddMinutes(-5));

        connection.UpdateTokens("new-access", "rotated", DateTimeOffset.UtcNow.AddHours(1));

        connection.RefreshToken.Should().Be("rotated");
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void Given_Expiry_When_IsExpired_Then_ComparesAgainstNow(int minutesFromNow, bool expected)
    {
        var now = DateTimeOffset.UtcNow;

        Connected(now.AddMinutes(minutesFromNow)).IsExpired(now).Should().Be(expected);
    }

    [Fact]
    public void Given_NoExpiry_When_IsExpired_Then_False()
    {
        Connected(null).IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }
}
