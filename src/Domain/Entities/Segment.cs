using Domain.Common;

namespace Domain.Entities;

public sealed class Segment : BaseEntity, IAuditableEntity
{
    private Segment()
    {
    }

    public Segment(Guid jobId, int segmentIndex, double startTime, double endTime, string originalText)
    {
        JobId = jobId;
        SegmentIndex = segmentIndex;
        StartTime = startTime;
        EndTime = endTime;
        OriginalText = originalText;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid JobId { get; private set; }
    public int SegmentIndex { get; private set; }
    public double StartTime { get; private set; }
    public double EndTime { get; private set; }
    public string OriginalText { get; private set; } = string.Empty;
    public string? AudioTextAi { get; private set; }
    public string? AudioTextEdited { get; private set; }
    public string? SubtitleTextAi { get; private set; }
    public string? SubtitleTextEdited { get; private set; }
    public bool IsEdited { get; private set; }
    public string? SpeakerLabel { get; private set; }
    public string? AssignedVoice { get; private set; }
    public string? TtsAudioPath { get; private set; }
    public long? TtsDurationMs { get; private set; }
    public bool NeedsTtsRegenerate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public double Duration => EndTime - StartTime;

    /// <summary>
    /// Text to be entered into TTS: COALESCE(AudioTextEdited, AudioTextAi, OriginalText). User-edited version is preferred; 
    /// unedited version is used for the AI ​​version; if no translation is available (audio = original), the original lyrics are used.
    /// </summary>
    public string TtsText =>
        FirstNonEmpty(AudioTextEdited, AudioTextAi) ?? OriginalText;

    /// <summary>
    /// Text for subtitles: COALESCE(SubtitleTextEdited, SubtitleTextAi, AudioTextEdited, AudioTextAi, OriginalText).
    /// If no separate subtitle is available, use the audio text; finally, use the original lyrics.
    /// </summary>
    public string SubtitleText =>
        FirstNonEmpty(SubtitleTextEdited, SubtitleTextAi, AudioTextEdited, AudioTextAi) ?? OriginalText;

    /// <summary>Assign AI translation (called during the Translate step). Does not affect user-edited version.</summary>
    public void SetAiTranslation(string? audioTextAi, string? subtitleTextAi)
    {
        AudioTextAi = audioTextAi;
        SubtitleTextAi = subtitleTextAi;
        Touch();
    }

    /// <summary>User edits the text for the voice track. If TTS has already been generated, mark it for regeneration.</summary>
    public void EditAudioText(string? text)
    {
        AudioTextEdited = text;
        IsEdited = true;

        // Only flag regeneration if a TTS clip already exists for this segment.
        if (TtsAudioPath is not null)
        {
            NeedsTtsRegenerate = true;
        }

        Touch();
    }

    /// <summary>User edits the text for subtitles. Does not affect TTS (subtitles are not read).</summary>
    public void EditSubtitleText(string? text)
    {
        SubtitleTextEdited = text;
        IsEdited = true;
        Touch();
    }

    /// <summary>Assign a specific voice to the segment (multi-speaker / manual assignment).</summary>
    public void AssignVoice(string? speakerLabel, string? voice)
    {
        SpeakerLabel = speakerLabel;
        AssignedVoice = voice;
        Touch();
    }

    /// <summary>Save the TTS result of the segment and clear the flag indicating it needs to be regenerated.</summary>
    public void SetTtsResult(string ttsAudioPath, long ttsDurationMs)
    {
        TtsAudioPath = ttsAudioPath;
        TtsDurationMs = ttsDurationMs;
        NeedsTtsRegenerate = false;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    // Returns the first non-empty candidate, mirroring SQL COALESCE over the text columns.
    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
