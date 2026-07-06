using Domain.Entities;

namespace Application.Helpers;

public static class SegmentEditApplier
{
    public static void Apply(Segment segment, string? audioText, string? subtitleText, string? assignedVoice)
    {
        if (audioText is not null)
        {
            segment.EditAudioText(audioText);
        }

        if (subtitleText is not null)
        {
            segment.EditSubtitleText(subtitleText);
        }

        if (assignedVoice is not null)
        {
            segment.AssignVoice(segment.SpeakerLabel, assignedVoice);
        }
    }
}
