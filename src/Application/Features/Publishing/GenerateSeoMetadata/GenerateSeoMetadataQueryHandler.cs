using Application.Interfaces;
using MediatR;

namespace Application.Features.Publishing.GenerateSeoMetadata;

public sealed class GenerateSeoMetadataQueryHandler : IRequestHandler<GenerateSeoMetadataQuery, GenerateSeoMetadataResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly ILlmCompletionService _llm;

    public GenerateSeoMetadataQueryHandler(IDubbingJobRepository jobs, ILlmCompletionService llm)
    {
        _jobs = jobs;
        _llm = llm;
    }

    public async Task<GenerateSeoMetadataResponse> Handle(GenerateSeoMetadataQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return GenerateSeoMetadataResponse.JobNotFound(request.JobId);
        }

        if (job.Segments.Count == 0)
        {
            return GenerateSeoMetadataResponse.GenerationFailed("The job has no transcript to summarize.");
        }

        var systemPrompt = SeoPromptBuilder.BuildSystemPrompt(job.AudioLanguage);
        var userPrompt = SeoPromptBuilder.BuildUserPrompt(job.Segments);

        var raw = await _llm.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        return SeoResponseParser.TryParse(raw, out var metadata, out var error)
            ? GenerateSeoMetadataResponse.Ok(metadata!)
            : GenerateSeoMetadataResponse.GenerationFailed(error!);
    }
}
