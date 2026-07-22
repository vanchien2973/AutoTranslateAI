using Application.Features.Providers.GetProviders;
using Application.Interfaces;

namespace Application.Tests.Providers;

public class GetProvidersQueryHandlerTests
{
    [Fact]
    public async Task Given_Registry_When_Handle_Then_ReturnsConfiguredProviders()
    {
        var registry = Substitute.For<IProviderRegistry>();
        registry.Current.Returns(new GetProvidersResponse("Azure", "WhisperNet", "OpenAI", "R2"));
        var handler = new GetProvidersQueryHandler(registry);

        var response = await handler.Handle(new GetProvidersQuery(), CancellationToken.None);

        response.Tts.Should().Be("Azure");
        response.SpeechToText.Should().Be("WhisperNet");
        response.Translation.Should().Be("OpenAI");
        response.Storage.Should().Be("R2");
    }
}
