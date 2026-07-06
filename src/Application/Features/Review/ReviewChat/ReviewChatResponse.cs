namespace Application.Features.Review.ReviewChat;

public sealed record ReviewChatResponse(
    ReviewChatStatus Status,
    string? AssistantMessage,
    IReadOnlyList<EditProposal> Proposals,
    string? Error)
{
    public static ReviewChatResponse Ok(string assistantMessage, IReadOnlyList<EditProposal> proposals) =>
        new(ReviewChatStatus.Ok, assistantMessage, proposals, null);

    public static ReviewChatResponse JobNotFound(Guid jobId) =>
        new(ReviewChatStatus.JobNotFound, null, [], $"Job {jobId} was not found.");

    public static ReviewChatResponse NotAwaitingReview() =>
        new(ReviewChatStatus.NotAwaitingReview, null, [], "The job can only be edited while it is in the review state.");

    public static ReviewChatResponse InvalidResponse(string error) =>
        new(ReviewChatStatus.InvalidResponse, null, [], error);
}
