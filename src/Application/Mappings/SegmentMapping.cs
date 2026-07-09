using Domain.Entities;

namespace Application.Mappings;

public static class SegmentMapping
{
    public static PipelineSegment ToPipeline(Segment segment) => new()
    {
        Index = segment.SegmentIndex,
        StartTime = segment.StartTime,
        EndTime = segment.EndTime,
        OriginalText = segment.OriginalText,
        AudioTextAi = segment.AudioTextAi,
        AudioTextEdited = segment.AudioTextEdited,
        SubtitleTextAi = segment.SubtitleTextAi,
        SubtitleTextEdited = segment.SubtitleTextEdited,
        AssignedVoice = segment.AssignedVoice,
        TtsAudioPath = segment.TtsAudioPath,
        TtsDurationMs = segment.TtsDurationMs,
        TtsVoice = segment.TtsVoice,
        NeedsTtsRegenerate = segment.NeedsTtsRegenerate,
    };

    public static SegmentDto ToDto(Segment segment) => new(
        segment.Id,
        segment.SegmentIndex,
        segment.StartTime,
        segment.EndTime,
        segment.OriginalText,
        segment.AudioTextAi,
        segment.AudioTextEdited,
        segment.SubtitleTextAi,
        segment.SubtitleTextEdited,
        segment.TtsText,
        segment.SubtitleText,
        segment.SpeakerLabel,
        segment.AssignedVoice,
        segment.IsEdited,
        segment.NeedsTtsRegenerate);

    public static Segment ToDomain(Guid jobId, PipelineSegment segment)
    {
        var entity = new Segment(jobId, segment.Index, segment.StartTime, segment.EndTime, segment.OriginalText);
        entity.SetAiTranslation(segment.AudioTextAi, segment.SubtitleTextAi);

        if (segment.AudioTextEdited is not null)
        {
            entity.EditAudioText(segment.AudioTextEdited);
        }

        if (segment.SubtitleTextEdited is not null)
        {
            entity.EditSubtitleText(segment.SubtitleTextEdited);
        }

        if (segment.AssignedVoice is not null)
        {
            entity.AssignVoice(null, segment.AssignedVoice);
        }

        return entity;
    }
}
