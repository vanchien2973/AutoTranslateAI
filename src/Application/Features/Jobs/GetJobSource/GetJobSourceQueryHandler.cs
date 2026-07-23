using Application.Interfaces;
using MediatR;

namespace Application.Features.Jobs.GetJobSource;

public sealed class GetJobSourceQueryHandler : IRequestHandler<GetJobSourceQuery, GetJobSourceResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly IWorkspaceManager _workspace;

    public GetJobSourceQueryHandler(IDubbingJobRepository jobs, IWorkspaceManager workspace)
    {
        _jobs = jobs;
        _workspace = workspace;
    }

    public async Task<GetJobSourceResponse> Handle(GetJobSourceQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null || string.IsNullOrWhiteSpace(job.SourceMediaFileName))
        {
            return GetJobSourceResponse.NotFound();
        }

        var path = _workspace.GetArtifactPath(job.Id, job.SourceMediaFileName);
        if (!File.Exists(path))
        {
            return GetJobSourceResponse.NotFound();
        }

        return GetJobSourceResponse.Ok(path, ContentTypeFor(job.SourceMediaFileName));
    }

    private static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".mp4" or ".m4v" => "video/mp4",
        ".webm" => "video/webm",
        ".mkv" => "video/x-matroska",
        ".mov" => "video/quicktime",
        ".ogv" => "video/ogg",
        _ => "application/octet-stream",
    };
}
