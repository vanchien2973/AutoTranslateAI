using Domain.Entities;

namespace Application.Helpers;

public static class SegmentEditApplier
{
    public static void Apply(
        Segment segment,
        string? audioText,
        string? subtitleText,
        string? speakerLabel,
        string? assignedVoice)
    {
        if (audioText is not null)
        {
            segment.EditAudioText(audioText);
        }

        if (subtitleText is not null)
        {
            segment.EditSubtitleText(subtitleText);
        }

        if (speakerLabel is not null || assignedVoice is not null)
        {
            segment.AssignVoice(speakerLabel ?? segment.SpeakerLabel, assignedVoice ?? segment.AssignedVoice);
        }
    }
}
