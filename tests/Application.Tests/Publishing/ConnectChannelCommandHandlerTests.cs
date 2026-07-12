using Application.Features.Publishing.ConnectChannel;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class ConnectChannelCommandHandlerTests
{
    private static IPlatformCredentialRepository CredsWith(PublishPlatform platform)
    {
        var creds = Substitute.For<IPlatformCredentialRepository>();
        creds.GetAsync(platform, Arg.Any<CancellationToken>())
            .Returns(new PlatformCredential(platform, "cid", "secret", null));
        return creds;
    }

    [Fact]
    public async Task Given_NoCredentials_When_Handle_Then_CredentialsMissing()
    {
        var creds = Substitute.For<IPlatformCredentialRepository>();
        creds.GetAsync(PublishPlatform.Facebook, Arg.Any<CancellationToken>()).Returns((PlatformCredential?)null);
        var handler = new ConnectChannelCommandHandler(creds, Substitute.For<IOAuthProviderFactory>(), Substitute.For<IChannelConnectionRepository>());

        var response = await handler.Handle(
            new ConnectChannelCommand(PublishPlatform.Facebook, "code", "https://app/cb"), CancellationToken.None);

        response.Status.Should().Be(ConnectChannelStatus.CredentialsMissing);
    }

    [Fact]
    public async Task Given_ExchangeThrows_When_Handle_Then_ExchangeFailed()
    {
        var provider = Substitute.For<IOAuthProvider>();
        provider.ExchangeCodeAsync(Arg.Any<OAuthAppCredentials>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ChannelTokens>(_ => throw new InvalidOperationException("bad code"));
        var factory = Substitute.For<IOAuthProviderFactory>();
        factory.Get(PublishPlatform.YouTube).Returns(provider);
        var handler = new ConnectChannelCommandHandler(CredsWith(PublishPlatform.YouTube), factory, Substitute.For<IChannelConnectionRepository>());

        var response = await handler.Handle(
            new ConnectChannelCommand(PublishPlatform.YouTube, "code", "https://app/cb"), CancellationToken.None);

        response.Status.Should().Be(ConnectChannelStatus.ExchangeFailed);
        response.Error.Should().Contain("bad code");
    }

    [Fact]
    public async Task Given_ValidCode_When_Handle_Then_SavesConnection()
    {
        var provider = Substitute.For<IOAuthProvider>();
        provider.ExchangeCodeAsync(Arg.Any<OAuthAppCredentials>(), "code", "https://app/cb", Arg.Any<CancellationToken>())
            .Returns(new ChannelTokens("chid", "My Channel", "token", "refresh", DateTimeOffset.UtcNow.AddHours(1)));
        var factory = Substitute.For<IOAuthProviderFactory>();
        factory.Get(PublishPlatform.YouTube).Returns(provider);
        var connections = Substitute.For<IChannelConnectionRepository>();
        var handler = new ConnectChannelCommandHandler(CredsWith(PublishPlatform.YouTube), factory, connections);

        var response = await handler.Handle(
            new ConnectChannelCommand(PublishPlatform.YouTube, "code", "https://app/cb"), CancellationToken.None);

        response.Status.Should().Be(ConnectChannelStatus.Ok);
        response.Connection!.ChannelName.Should().Be("My Channel");
        await connections.Received(1).AddAsync(
            Arg.Is<ChannelConnection>(c => c.ChannelId == "chid" && c.AccessToken == "token"), Arg.Any<CancellationToken>());
        await connections.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
