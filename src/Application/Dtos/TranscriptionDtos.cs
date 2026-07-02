namespace Application.Dtos;

public sealed record TranscriptSegment(int Index, double StartTime, double EndTime, string Text);
public sealed record TranscriptionResult(IReadOnlyList<TranscriptSegment> Segments, string DetectedLanguage);
