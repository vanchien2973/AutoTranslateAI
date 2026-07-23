using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Workers.Steps;

namespace Workers.Tests.Steps;

public class TtsStepTests
{
    private static (ITtsService Tts, IAudioTimelineAssembler Assembler) Media(string vocalsPath = "/ws/dubbed_vocals.wav")
    {
        var tts = Substitute.For<ITtsService>();
        tts.SynthesizeAsync(Arg.Any<TtsRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new TtsResult(callInfo.Arg<TtsRequest>().OutputPath, 1000, "vi-VN-HoaiMyNeural"));
        var assembler = Substitute.For<IAudioTimelineAssembler>();
        assembler.AssembleAsync(Arg.Any<TimelineAssemblyRequest>(), Arg.Any<CancellationToken>()).Returns(vocalsPath);
        return (tts, assembler);
    }

    private static TtsStep Step(ITtsService tts, IAudioTimelineAssembler assembler) =>
        new(tts, assembler, StepFixture.Workspace(), NullLogger<TtsStep>.Instance);

    [Fact]
    public async Task Given_DubbingDisabled_When_Execute_Then_Skips()
    {
        var (tts, assembler) = Media();
        var result = await Step(tts, assembler).ExecuteAsync(StepFixture.Context(enableDubbing: false), CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_NoSegments_When_Execute_Then_Skips()
    {
        var (tts, assembler) = Media();
        var result = await Step(tts, assembler).ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_FreshSegment_When_Execute_Then_SynthesizesAndAssemblesVocals()
    {
        var (tts, assembler) = Media();
        var context = StepFixture.Context();
        context.Segments.Add(StepFixture.Segment(0, "hello", 0, 1));

        var result = await Step(tts, assembler).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.DubbedVocalsPath.Should().Be("/ws/dubbed_vocals.wav");
        context.Segments[0].TtsVoice.Should().Be("vi-VN-HoaiMyNeural");
        context.Segments[0].TtsAudioPath.Should().NotBeNull();
        await tts.ReceivedWithAnyArgs().SynthesizeAsync(default!, default);
    }

    [Fact]
    public async Task Given_UnchangedClipWithMatchingVoice_When_Execute_Then_ReusesWithoutSynthesizing()
    {
        var (tts, assembler) = Media();
        var context = StepFixture.Context();
        var segment = StepFixture.Segment(0, "hello", 0, 1);
        segment.TtsAudioPath = "/ws/tts/seg-0000.wav";
        segment.TtsVoice = "vi-VN-NamMinhNeural";
        segment.AssignedVoice = "vi-VN-NamMinhNeural";
        segment.NeedsTtsRegenerate = false;
        context.Segments.Add(segment);

        var result = await Step(tts, assembler).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.DubbedVocalsPath.Should().Be("/ws/dubbed_vocals.wav");
        await tts.DidNotReceiveWithAnyArgs().SynthesizeAsync(default!, default);
    }
}

public class GenSubtitleStepTests
{
    private static (IWorkspaceManager Workspace, string Path) TempWorkspace()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ata-sub-{Guid.NewGuid():N}.srt");
        var workspace = Substitute.For<IWorkspaceManager>();
        workspace.GetArtifactPath(Arg.Any<Guid>(), Arg.Any<string>()).Returns(path);
        return (workspace, path);
    }

    [Fact]
    public async Task Given_SubtitleModeNone_When_Execute_Then_Skips()
    {
        var step = new GenSubtitleStep(StepFixture.Workspace(), NullLogger<GenSubtitleStep>.Instance);
        var context = StepFixture.Context(subtitleMode: SubtitleMode.None);
        context.Segments.Add(StepFixture.Segment(0));

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_NoSegments_When_Execute_Then_Skips()
    {
        var step = new GenSubtitleStep(StepFixture.Workspace(), NullLogger<GenSubtitleStep>.Instance);
        var context = StepFixture.Context(subtitleMode: SubtitleMode.Softsub);

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_SubtitleModeAndSegments_When_Execute_Then_WritesSrtAndSetsPath()
    {
        var (workspace, path) = TempWorkspace();
        try
        {
            var context = StepFixture.Context(subtitleMode: SubtitleMode.Softsub);
            var segment = StepFixture.Segment(0, "hello", 0, 1);
            segment.SubtitleTextAi = "xin chào";
            context.Segments.Add(segment);

            var result = await new GenSubtitleStep(workspace, NullLogger<GenSubtitleStep>.Instance)
                .ExecuteAsync(context, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            context.SubtitlePath.Should().Be(path);
            File.Exists(path).Should().BeTrue();
            (await File.ReadAllTextAsync(path)).Should().Contain("xin chào");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

public class MixStepTests
{
    [Fact]
    public async Task Given_NoDubbedVocals_When_Execute_Then_Skips()
    {
        var step = new MixStep(Substitute.For<IAudioMixer>(), StepFixture.Workspace());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task Given_BgmNone_When_Execute_Then_UsesDubbedVocalsAsFinalTrack()
    {
        var mixer = Substitute.For<IAudioMixer>();
        var context = StepFixture.Context(bgmMode: BgmMode.None);
        context.DubbedVocalsPath = "/ws/dubbed_vocals.wav";

        var result = await new MixStep(mixer, StepFixture.Workspace()).ExecuteAsync(context, CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
        context.MixedAudioPath.Should().Be("/ws/dubbed_vocals.wav");
        await mixer.DidNotReceiveWithAnyArgs().MixAsync(default!, default);
    }

    [Fact]
    public async Task Given_DemucsBgm_When_Execute_Then_MixesWithAccompaniment()
    {
        var mixer = Substitute.For<IAudioMixer>();
        mixer.MixAsync(Arg.Any<MixRequest>(), Arg.Any<CancellationToken>()).Returns("/ws/mixed.wav");
        var context = StepFixture.Context(bgmMode: BgmMode.DemucsAI);
        context.DubbedVocalsPath = "/ws/dubbed_vocals.wav";
        context.BackgroundMusicPath = "/ws/stems/no_vocals.wav";

        var result = await new MixStep(mixer, StepFixture.Workspace()).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.MixedAudioPath.Should().Be("/ws/mixed.wav");
        await mixer.Received(1).MixAsync(
            Arg.Is<MixRequest>(r => r.BackgroundMusicPath == "/ws/stems/no_vocals.wav"), Arg.Any<CancellationToken>());
    }
}

public class RenderStepTests
{
    [Fact]
    public async Task Given_NoSourceVideo_When_Execute_Then_Fails()
    {
        var step = new RenderStep(Substitute.For<IVideoRenderer>(), StepFixture.Workspace(), Substitute.For<IStorageService>(), NullLogger<RenderStep>.Instance);

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_DubbingEnabledButNoAudio_When_Execute_Then_Fails()
    {
        var context = StepFixture.Context(enableDubbing: true);
        context.SourceVideoPath = "/ws/video.mp4";

        var result = await new RenderStep(Substitute.For<IVideoRenderer>(), StepFixture.Workspace(), Substitute.For<IStorageService>(), NullLogger<RenderStep>.Instance)
            .ExecuteAsync(context, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_SourceAndAudio_When_Execute_Then_RendersWithMixedAudio()
    {
        var renderer = Substitute.For<IVideoRenderer>();
        renderer.RenderAsync(Arg.Any<RenderRequest>(), Arg.Any<CancellationToken>()).Returns("/ws/output.mp4");
        var context = StepFixture.Context();
        context.SourceVideoPath = "/ws/video.mp4";
        context.DubbedVocalsPath = "/ws/dubbed_vocals.wav";

        var result = await new RenderStep(renderer, StepFixture.Workspace(), Substitute.For<IStorageService>(), NullLogger<RenderStep>.Instance).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.OutputVideoPath.Should().Be("/ws/output.mp4");
        await renderer.Received(1).RenderAsync(
            Arg.Is<RenderRequest>(r => r.AudioPath == "/ws/dubbed_vocals.wav"), Arg.Any<CancellationToken>());
    }
}

public class UploadStepTests
{
    [Fact]
    public async Task Given_NoOutput_When_Execute_Then_Fails()
    {
        var step = new UploadStep(Substitute.For<IStorageService>());

        var result = await step.ExecuteAsync(StepFixture.Context(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Output_When_Execute_Then_UploadsUnderJobKey_AndSetsUrl()
    {
        var storage = Substitute.For<IStorageService>();
        storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn/output.mp4");
        var context = StepFixture.Context();
        context.OutputVideoPath = "/ws/output.mp4";

        var result = await new UploadStep(storage).ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.OutputUrl.Should().Be("https://cdn/output.mp4");
        context.OutputStorageKey.Should().Be(OutputStorageKey.For(context.JobId));
        await storage.Received(1).UploadAsync(
            "/ws/output.mp4", OutputStorageKey.For(context.JobId), Arg.Any<CancellationToken>());
    }
}
