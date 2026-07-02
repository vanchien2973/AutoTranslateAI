namespace Application.Pipeline;

public static class SegmentText
{
    public static string ForTts(PipelineSegment segment) =>
        segment.AudioTextEdited ?? segment.AudioTextAi ?? segment.OriginalText;
    public static string ForSubtitle(PipelineSegment segment) =>
        segment.SubtitleTextEdited
        ?? segment.SubtitleTextAi
        ?? segment.AudioTextEdited
        ?? segment.AudioTextAi
        ?? segment.OriginalText;
}
