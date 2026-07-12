using Application.Interfaces;
using Application.Publishing;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class PublishExecutorTests
{
    private static PublishExecutor Build(
        IChannelConnectionRepository connections,
        IPublisherFactory publishers = null!,
        IPublishResultRepository results = null!) =>
        new(connections,
            publishers ?? Substitute.For<IPublisherFactory>(),
            results ?? Substitute.For<IPublishResultRepository>());

    private static PublishTarget YouTubeTarget => new(PublishPlatform.YouTube, null, "Title", null, null);

    [Fact]
    public async Task Given_NoConnectedAccount_When_Execute_Then_TargetFailed()
    {
        var connections = Substitute.For<IChannelConnectionRepository>();
        connections.GetLatestAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>()).Returns((ChannelConnection?)null);
        var executor = Build(connections);

        var results = await executor.ExecuteAsync(TestJobs.Completed(), [YouTubeTarget], CancellationToken.None);

        results.Should().ContainSingle().Which.Status.Should().Be(PublishStatus.Failed);
    }

    [Fact]
    public async Task Given_ConnectedAccount_When_PublisherSucceeds_Then_PublishedAndPersisted()
    {
        var connection = new ChannelConnection(PublishPlatform.YouTube, "chid", "name", "token", null, null);
        var connections = Substitute.For<IChannelConnectionRepository>();
        connections.GetLatestAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>()).Returns(connection);

        var publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PublishOutcome("ext-1", "https://youtu.be/ext-1"));
        var publishers = Substitute.For<IPublisherFactory>();
        publishers.Get(PublishPlatform.YouTube).Returns(publisher);

        var repo = Substitute.For<IPublishResultRepository>();
        var executor = Build(connections, publishers, repo);

        var results = await executor.ExecuteAsync(TestJobs.Completed(), [YouTubeTarget], CancellationToken.None);

        var result = results.Should().ContainSingle().Subject;
        result.Status.Should().Be(PublishStatus.Published);
        result.ExternalId.Should().Be("ext-1");
        result.Url.Should().Be("https://youtu.be/ext-1");
        await repo.Received(1).AddAsync(Arg.Any<PublishResult>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_PublisherThrows_When_Execute_Then_TargetFailedWithError()
    {
        var connection = new ChannelConnection(PublishPlatform.YouTube, "chid", "name", "token", null, null);
        var connections = Substitute.For<IChannelConnectionRepository>();
        connections.GetLatestAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>()).Returns(connection);

        var publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
            .Returns<PublishOutcome>(_ => throw new InvalidOperationException("quota exceeded"));
        var publishers = Substitute.For<IPublisherFactory>();
        publishers.Get(PublishPlatform.YouTube).Returns(publisher);

        var executor = Build(connections, publishers);

        var results = await executor.ExecuteAsync(TestJobs.Completed(), [YouTubeTarget], CancellationToken.None);

        var result = results.Should().ContainSingle().Subject;
        result.Status.Should().Be(PublishStatus.Failed);
        result.Error.Should().Contain("quota exceeded");
    }
}
