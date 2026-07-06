using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Review.ReviewChat;

public sealed class ReviewChatCommandHandler : IRequestHandler<ReviewChatCommand, ReviewChatResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly ILlmCompletionService _llm;
    private readonly IReviewSessionStore _sessions;

    public ReviewChatCommandHandler(IDubbingJobRepository jobs, ILlmCompletionService llm, IReviewSessionStore sessions)
    {
        _jobs = jobs;
        _llm = llm;
        _sessions = sessions;
    }

    public async Task<ReviewChatResponse> Handle(ReviewChatCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return ReviewChatResponse.JobNotFound(request.JobId);
        }

        // Segments are locked for render outside review; the chatbox only proposes while awaiting review.
        if (job.Status != JobStatus.AwaitingReview)
        {
            return ReviewChatResponse.NotAwaitingReview();
        }

        var history = _sessions.GetHistory(request.JobId);
        var relevant = SegmentSelector.Pick(job.Segments, request.UserMessage);
        var systemPrompt = ReviewPromptBuilder.BuildSystemPrompt(job.SourceLanguage, job.AudioLanguage, job.SubtitleLanguage);
        var userPrompt = ReviewPromptBuilder.BuildUserPrompt(relevant, history, request.UserMessage);

        var raw = await _llm.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        // Never trust raw LLM output: parse strictly against real segments before exposing proposals.
        if (!ReviewResponseParser.TryParse(raw, job.Segments, out var message, out var proposals, out var error))
        {
            return ReviewChatResponse.InvalidResponse(error!);
        }

        // Persist the turn + pending proposals so history and apply work within the review session.
        _sessions.AppendHistory(request.JobId, new ChatMessage(ChatRole.User, request.UserMessage));
        _sessions.AppendHistory(request.JobId, new ChatMessage(ChatRole.Assistant, message!));
        _sessions.SaveProposals(request.JobId, proposals!);

        return ReviewChatResponse.Ok(message!, proposals!);
    }
}
