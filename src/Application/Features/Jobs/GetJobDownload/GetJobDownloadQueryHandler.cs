using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Jobs.GetJobDownload;

public sealed class GetJobDownloadQueryHandler : IRequestHandler<GetJobDownloadQuery, GetJobDownloadResponse>
{
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    private readonly IDubbingJobRepository _jobs;
    private readonly IStorageService _storage;

    public GetJobDownloadQueryHandler(IDubbingJobRepository jobs, IStorageService storage)
    {
        _jobs = jobs;
        _storage = storage;
    }

    public async Task<GetJobDownloadResponse> Handle(GetJobDownloadQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return GetJobDownloadResponse.NotFound();
        }

        if (job.Status != JobStatus.Completed || string.IsNullOrEmpty(job.OutputFilePath))
        {
            return GetJobDownloadResponse.Conflict("Output is not ready yet.");
        }

        var url = await _storage.GetPresignedUrlAsync(OutputStorageKey.For(job.Id), DownloadUrlLifetime, cancellationToken);
        return GetJobDownloadResponse.Ok(url, (int)DownloadUrlLifetime.TotalSeconds);
    }
}
