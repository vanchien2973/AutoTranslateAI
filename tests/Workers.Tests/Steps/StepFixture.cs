using Application.Interfaces;
using Application.Pipeline;

namespace Workers.Tests.Steps;

internal static class StepFixture
{
    public static PipelineContext Context(
        bool enableDubbing = true,
        SubtitleMode subtitleMode = SubtitleMode.None,
        BgmMode bgmMode = BgmMode.DemucsAI,
        string audioLanguage = "vi",
        string subtitleLanguage = "vi") =>
        new()
        {
            JobId = Guid.NewGuid(),
            WorkspacePath = "/ws",
            SourceUrl = "https://src/video",
            AudioLanguage = audioLanguage,
            SubtitleLanguage = subtitleLanguage,
            EnableDubbing = enableDubbing,
            SubtitleMode = subtitleMode,
            BgmMode = bgmMode,
        };

    public static IWorkspaceManager Workspace()
    {
        var workspace = Substitute.For<IWorkspaceManager>();
        workspace.GetArtifactPath(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(callInfo => $"/ws/{callInfo.ArgAt<string>(1)}");
        return workspace;
    }

    public static PipelineSegment Segment(int index = 0, string text = "hello", double start = 0, double end = 1) =>
        new() { Index = index, StartTime = start, EndTime = end, OriginalText = text };
}
