using Application.Features.Publishing.GetChannels;
using Application.Features.Publishing.GetPlatformCredentials;
using Application.Features.Publishing.GetPublishResults;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class PublishingQueryHandlersTests
{
    [Fact]
    public async Task GetChannels_MapsConnections()
    {
        var connections = Substitute.For<IChannelConnectionRepository>();
        connections.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<ChannelConnection>
        {
            new(PublishPlatform.YouTube, "chid", "My Channel", "token", null, null),
        });

        var response = await new GetChannelsQueryHandler(connections)
            .Handle(new GetChannelsQuery(), CancellationToken.None);

        response.Channels.Should().ContainSingle().Which.ChannelName.Should().Be("My Channel");
    }

    [Fact]
    public async Task GetPlatformCredentials_MasksSecret()
    {
        var credentials = Substitute.For<IPlatformCredentialRepository>();
        credentials.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<PlatformCredential>
        {
            new(PublishPlatform.TikTok, "cid", "super-secret", "https://cb"),
        });

        var response = await new GetPlatformCredentialsQueryHandler(credentials)
            .Handle(new GetPlatformCredentialsQuery(), CancellationToken.None);

        var dto = response.Credentials.Should().ContainSingle().Subject;
        dto.ClientId.Should().Be("cid");
        dto.HasSecret.Should().BeTrue();
    }

    [Fact]
    public async Task GetPublishResults_MapsResults()
    {
        var result = new PublishResult(Guid.NewGuid(), PublishPlatform.Facebook);
        result.MarkPublished("ext", "https://fb/x");
        var results = Substitute.For<IPublishResultRepository>();
        results.ListByJobAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<PublishResult> { result });

        var response = await new GetPublishResultsQueryHandler(results)
            .Handle(new GetPublishResultsQuery(Guid.NewGuid()), CancellationToken.None);

        response.Results.Should().ContainSingle().Which.Status.Should().Be(PublishStatus.Published);
    }
}
