using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Admin.CleanupStorage;

public sealed class CleanupStorageCommandHandler : IRequestHandler<CleanupStorageCommand, CleanupStorageResponse>
{
    private const string LogoPrefix = "logos/";
    private const string OutputSuffix = "/output.mp4";

    private readonly IStorageService _storage;
    private readonly IDubbingJobRepository _jobs;
    private readonly ILogger<CleanupStorageCommandHandler> _logger;

    public CleanupStorageCommandHandler(
        IStorageService storage,
        IDubbingJobRepository jobs,
        ILogger<CleanupStorageCommandHandler> logger)
    {
        _storage = storage;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<CleanupStorageResponse> Handle(CleanupStorageCommand request, CancellationToken cancellationToken)
    {
        var liveRefs = await _jobs.ListActiveStorageRefsAsync(cancellationToken);
        var protectedLogoKeys = liveRefs
            .Select(reference => reference.LogoStorageKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal)!;
        var protectedJobIds = liveRefs.Select(reference => reference.JobId).ToHashSet();

        var keys = await _storage.ListKeysAsync(string.Empty, cancellationToken);

        var logosToDelete = new List<string>();
        var outputsToDelete = new List<string>();

        foreach (var key in keys)
        {
            if (key.StartsWith(LogoPrefix, StringComparison.Ordinal))
            {
                if (!protectedLogoKeys.Contains(key))
                {
                    logosToDelete.Add(key);
                }
            }
            else if (key.EndsWith(OutputSuffix, StringComparison.Ordinal))
            {
                var idPart = key[..^OutputSuffix.Length];
                // Only touch keys that look like {jobId}/output.mp4; leave anything unrecognized alone.
                if (Guid.TryParseExact(idPart, "N", out var jobId) && !protectedJobIds.Contains(jobId))
                {
                    outputsToDelete.Add(key);
                }
            }
        }

        if (!request.DryRun)
        {
            foreach (var key in logosToDelete.Concat(outputsToDelete))
            {
                try
                {
                    await _storage.DeleteAsync(key, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to delete R2 object {Key}", key);
                }
            }
        }

        _logger.LogInformation(
            "Storage cleanup ({Mode}): {Logos} logo(s), {Outputs} output(s)",
            request.DryRun ? "dry-run" : "delete", logosToDelete.Count, outputsToDelete.Count);

        return new CleanupStorageResponse(
            request.DryRun,
            logosToDelete.Count,
            outputsToDelete.Count,
            logosToDelete.Concat(outputsToDelete).ToList());
    }
}
