using Domain.Entities;

namespace Application.Tests.Mappings;

public class SegmentMappingTests
{
    private static Segment BuildEdited()
    {
        var segment = new Segment(Guid.NewGuid(), 2, 1.0, 3.0, "original");
        segment.SetAiTranslation("audio ai", "sub ai");
        segment.EditAudioText("audio edit");
        segment.EditSubtitleText("sub edit");
        segment.AssignVoice("Speaker A", "vi-VN-NamMinhNeural");
        segment.SetTtsResult("/tts/seg.wav", 1234, "vi-VN-NamMinhNeural");
        return segment;
    }

    [Fact]
    public void ToPipeline_CopiesAllFields()
    {
        var segment = BuildEdited();

        var pipeline = SegmentMapping.ToPipeline(segment);

        pipeline.Index.Should().Be(2);
        pipeline.OriginalText.Should().Be("original");
        pipeline.AudioTextAi.Should().Be("audio ai");
        pipeline.AudioTextEdited.Should().Be("audio edit");
        pipeline.SubtitleTextEdited.Should().Be("sub edit");
        pipeline.AssignedVoice.Should().Be("vi-VN-NamMinhNeural");
        pipeline.TtsAudioPath.Should().Be("/tts/seg.wav");
        pipeline.TtsDurationMs.Should().Be(1234);
        pipeline.TtsVoice.Should().Be("vi-VN-NamMinhNeural");
    }

    [Fact]
    public void ToDto_ResolvesTextsAndFlags()
    {
        var segment = BuildEdited();

        var dto = SegmentMapping.ToDto(segment);

        dto.SegmentIndex.Should().Be(2);
        dto.TtsText.Should().Be("audio edit");       // edited wins
        dto.SubtitleText.Should().Be("sub edit");
        dto.SpeakerLabel.Should().Be("Speaker A");
        dto.AssignedVoice.Should().Be("vi-VN-NamMinhNeural");
        dto.IsEdited.Should().BeTrue();
    }

    [Fact]
    public void ToDomain_RebuildsEntityFromPipeline()
    {
        var jobId = Guid.NewGuid();
        var pipeline = new PipelineSegment
        {
            Index = 5,
            StartTime = 0,
            EndTime = 2,
            OriginalText = "hello",
            AudioTextAi = "xin chào",
            SubtitleTextAi = "phụ đề",
            AudioTextEdited = "xin chào (sửa)",
            SubtitleTextEdited = "phụ đề (sửa)",
            AssignedVoice = "vi-VN-HoaiMyNeural",
        };

        var entity = SegmentMapping.ToDomain(jobId, pipeline);

        entity.JobId.Should().Be(jobId);
        entity.SegmentIndex.Should().Be(5);
        entity.OriginalText.Should().Be("hello");
        entity.AudioTextAi.Should().Be("xin chào");
        entity.AudioTextEdited.Should().Be("xin chào (sửa)");
        entity.SubtitleTextEdited.Should().Be("phụ đề (sửa)");
        entity.AssignedVoice.Should().Be("vi-VN-HoaiMyNeural");
        entity.IsEdited.Should().BeTrue();
    }
}
