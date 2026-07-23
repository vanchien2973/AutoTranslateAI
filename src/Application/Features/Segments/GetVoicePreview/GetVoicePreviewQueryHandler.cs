using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Segments.GetVoicePreview;

public sealed class GetVoicePreviewQueryHandler : IRequestHandler<GetVoicePreviewQuery, GetVoicePreviewResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly ITtsService _tts;
    private readonly IWorkspaceManager _workspace;

    public GetVoicePreviewQueryHandler(IDubbingJobRepository jobs, ITtsService tts, IWorkspaceManager workspace)
    {
        _jobs = jobs;
        _tts = tts;
        _workspace = workspace;
    }

    public async Task<GetVoicePreviewResponse> Handle(GetVoicePreviewQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return GetVoicePreviewResponse.NotFound();
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return GetVoicePreviewResponse.Conflict();
        }

        var segment = job.Segments.FirstOrDefault(candidate => candidate.Id == request.SegmentId);
        var text = segment?.TtsText;
        if (segment is null || string.IsNullOrWhiteSpace(text))
        {
            return GetVoicePreviewResponse.NotFound();
        }

        var cachePath = _workspace.GetArtifactPath(
            job.Id, $"voice-preview/{request.SegmentId:N}-{ShortHash(text)}.wav");

        if (!File.Exists(cachePath))
        {
            PruneStale(cachePath, request.SegmentId);

            var tempPath = cachePath + ".tmp";
            try
            {
                await _tts.SynthesizeAsync(
                    new TtsRequest(text, job.AudioLanguage, job.VoiceGender, segment.AssignedVoice, 1.0, tempPath),
                    cancellationToken);
                File.Move(tempPath, cachePath, overwrite: true);
            }
            catch (Exception)
            {
                TryDelete(tempPath);
                throw;
            }
        }

        var audio = await File.ReadAllBytesAsync(cachePath, cancellationToken);
        return GetVoicePreviewResponse.Ok(audio);
    }

    private static string ShortHash(string text)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(digest.AsSpan(0, 8)).ToLowerInvariant();
    }

    private static void PruneStale(string cachePath, Guid segmentId)
    {
        var directory = Path.GetDirectoryName(cachePath);
        if (directory is null || !Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, $"{segmentId:N}-*.wav"))
        {
            if (!string.Equals(file, cachePath, StringComparison.Ordinal))
            {
                TryDelete(file);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a locked leftover file is harmless.
        }
    }
}
