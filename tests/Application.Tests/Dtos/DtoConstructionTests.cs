using Domain.Enums;

namespace Application.Tests.Dtos;

public class DtoConstructionTests
{
    [Fact]
    public void JobStepDto_And_JobStatusDto_HoldFields()
    {
        var step = new JobStepDto("Tts", "Completed", 2, 1500, 0, null);
        var status = new JobStatusDto(
            Guid.NewGuid(), "ProcessingPhase2", "vi", "vi", true, "Tts", 55, null, "out.mp4", "https://dl",
            SegmentCount: 3, EditedSegmentCount: 1,
            CreatedAt: DateTimeOffset.UtcNow, StartedAt: null, ReviewReadyAt: null,
            ConfirmedAt: null, CompletedAt: null, Steps: [step],
            SubtitleMode: SubtitleMode.Hardsub, SubtitleFontFamily: "Noto Sans", SubtitleFontSize: 28,
            SubtitlePosition: SubtitlePosition.Bottom, SubtitleBold: true, SubtitleItalic: false);

        status.Status.Should().Be("ProcessingPhase2");
        status.ProgressPercent.Should().Be(55);
        status.Steps.Should().ContainSingle().Which.StepType.Should().Be("Tts");
    }

    [Fact]
    public void UsageEntry_Defaults_OutputZeroAndNoJob()
    {
        var entry = new UsageEntry("OpenAI", "Translate", UsageUnit.Tokens, 1000);

        entry.OutputUnits.Should().Be(0);
        entry.JobId.Should().BeNull();
        entry.Unit.Should().Be(UsageUnit.Tokens);
    }
}
