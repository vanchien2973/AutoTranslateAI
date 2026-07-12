using Application.Interfaces;
using Workers.Steps;

namespace Workers.Tests.Steps;

public class DownloadStepTests
{
    [Fact]
    public async Task Given_SourceUrl_When_Execute_Then_SetsSourceVideoPath()
    {
        var downloader = Substitute.For<IVideoDownloader>();
        downloader.DownloadAsync(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DownloadResult("/ws/video.mp4", "Title", 42));
        var context = StepFixture.Context();

        var result = await new DownloadStep(downloader).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.SourceVideoPath.Should().Be("/ws/video.mp4");
        await downloader.Received(1).DownloadAsync(
            Arg.Is<DownloadRequest>(r => r.Url == context.SourceUrl && r.OutputDirectory == context.WorkspacePath),
            Arg.Any<CancellationToken>());
    }
}

public class ExtractAudioStepTests
{
    [Fact]
    public async Task Given_NoSourceVideo_When_Execute_Then_Fails()
    {
        var step = new ExtractAudioStep(Substitute.For<IAudioExtractor>(), StepFixture.Workspace());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_SourceVideo_When_Execute_Then_SetsAudioPath()
    {
        var extractor = Substitute.For<IAudioExtractor>();
        extractor.ExtractAudioAsync(Arg.Any<AudioExtractionRequest>(), Arg.Any<CancellationToken>())
            .Returns("/ws/audio.wav");
        var context = StepFixture.Context();
        context.SourceVideoPath = "/ws/video.mp4";

        var result = await new ExtractAudioStep(extractor, StepFixture.Workspace()).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.AudioPath.Should().Be("/ws/audio.wav");
    }
}

public class SeparateBgmStepTests
{
    [Fact]
    public async Task Given_NoAudio_When_Execute_Then_Fails()
    {
        var step = new SeparateBgmStep(Substitute.For<IDemucsService>(), StepFixture.Workspace());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Audio_When_Execute_Then_SetsVocalsAndBackground()
    {
        var demucs = Substitute.For<IDemucsService>();
        demucs.SeparateAsync(Arg.Any<DemucsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DemucsResult("/ws/stems/vocals.wav", "/ws/stems/no_vocals.wav"));
        var context = StepFixture.Context();
        context.AudioPath = "/ws/audio.wav";

        var result = await new SeparateBgmStep(demucs, StepFixture.Workspace()).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.VocalsPath.Should().Be("/ws/stems/vocals.wav");
        context.BackgroundMusicPath.Should().Be("/ws/stems/no_vocals.wav");
    }
}

public class TranscribeStepTests
{
    private static ISpeechToTextService SttReturning(TranscriptionResult result)
    {
        var stt = Substitute.For<ISpeechToTextService>();
        stt.TranscribeAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(result);
        return stt;
    }

    [Fact]
    public async Task Given_NoAudioOrVocals_When_Execute_Then_Fails()
    {
        var step = new TranscribeStep(SttReturning(new TranscriptionResult([], "en")),
            Substitute.For<IAudioExtractor>(), StepFixture.Workspace());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Vocals_When_Execute_Then_PopulatesSegments_AndDetectsLanguage()
    {
        var stt = SttReturning(new TranscriptionResult(
            [new TranscriptSegment(0, 0, 1.5, "hello"), new TranscriptSegment(1, 1.5, 3, "world")], "en"));
        var extractor = Substitute.For<IAudioExtractor>();
        var context = StepFixture.Context();
        context.VocalsPath = "/ws/vocals.wav";
        context.AudioPath = "/ws/audio.wav";

        var result = await new TranscribeStep(stt, extractor, StepFixture.Workspace())
            .ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.SourceLanguage.Should().Be("en");
        context.Segments.Select(s => s.OriginalText).Should().Equal("hello", "world");
        await extractor.Received(1).ExtractAudioAsync(
            Arg.Is<AudioExtractionRequest>(r => r.InputVideoPath == "/ws/vocals.wav"), Arg.Any<CancellationToken>());
    }
}

public class TranslateStepTests
{
    [Fact]
    public async Task Given_NoSegments_When_Execute_Then_Skips()
    {
        var step = new TranslateStep(Substitute.For<ITranslationService>());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_SourceEqualsAudio_And_NoSubtitleTranslation_When_Execute_Then_Skips_NoLlmCall()
    {
        var translation = Substitute.For<ITranslationService>();
        var context = StepFixture.Context(audioLanguage: "vi", subtitleLanguage: "vi");
        context.SourceLanguage = "vi";
        context.Segments.Add(StepFixture.Segment(0));

        var result = await new TranslateStep(translation).ExecuteAsync(context, CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
        await translation.DidNotReceiveWithAnyArgs()
            .TranslateBatchAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Given_ForeignSource_When_Execute_Then_TranslatesAudioText()
    {
        var translation = Substitute.For<ITranslationService>();
        translation.TranslateBatchAsync(Arg.Any<IReadOnlyList<string>>(), "en", "vi", Arg.Any<CancellationToken>())
            .Returns(["xin chào"]);
        var context = StepFixture.Context(audioLanguage: "vi", subtitleLanguage: "vi");
        context.SourceLanguage = "en";
        context.Segments.Add(StepFixture.Segment(0, "hello"));

        var result = await new TranslateStep(translation).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Segments[0].AudioTextAi.Should().Be("xin chào");
    }
}
